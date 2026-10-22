// Author: Anwarul Haque (@HYPERMX7) | Contact: 7739399007
using System.Windows;
using HyperPDF.Models;
using HyperPDF.Services;

namespace HyperPDF.ViewModels;

/// <summary>
/// View model behind the update dialog. It checks the update manifest, shows the details of
/// a newer release, downloads the installer with progress feedback and finally launches it.
/// </summary>
public sealed class UpdateViewModel : ViewModelBase
{
    private readonly IUpdateService _updates;
    private readonly ISettingsService _settings;
    private UpdateManifest? _manifest;
    private string _statusText = "Checking for updates…";
    private string _errorDetail = string.Empty;
    private double _progressPercent;
    private bool _isIndeterminate = true;
    private bool _isChecking;
    private bool _hasUpdate;
    private bool _isDownloading;
    private bool _hasError;
    private CancellationTokenSource? _downloadCancellation;

    /// <summary>Creates the view model over the given services.</summary>
    public UpdateViewModel(IUpdateService updates, ISettingsService settings, UpdateManifest? preloadedManifest = null)
    {
        _updates = updates;
        _settings = settings;
        _manifest = preloadedManifest;

        CheckForUpdatesCommand = new AsyncRelayCommand(() => CheckForUpdatesAsync(), () => !IsBusy);
        UpdateNowCommand = new AsyncRelayCommand(() => RunUpdateNowAsync(), () => HasUpdate && !IsDownloading);
        SkipVersionCommand = new RelayCommand(SkipVersion, () => HasUpdate);
        CancelCommand = new RelayCommand(Cancel, () => true);

        if (_manifest is not null)
        {
            _statusText = $"HyperPDF {_manifest.Version} is available.";
            _hasUpdate = true;
        }
    }

    /// <summary>Raised when the dialog should be closed.</summary>
    public event EventHandler? CloseRequested;

    /// <summary>Command performing the update check.</summary>
    public AsyncRelayCommand CheckForUpdatesCommand { get; }

    /// <summary>Command that downloads the installer and starts the update.</summary>
    public AsyncRelayCommand UpdateNowCommand { get; }

    /// <summary>Command remembering the current version as skipped.</summary>
    public RelayCommand SkipVersionCommand { get; }

    /// <summary>Command aborting a download or closing the dialog.</summary>
    public RelayCommand CancelCommand { get; }

    /// <summary>The manifest of the newer release, when one was found.</summary>
    public UpdateManifest? Manifest => _manifest;

    /// <summary>Status line shown in the dialog.</summary>
    public string StatusText
    {
        get => _statusText;
        private set => SetProperty(ref _statusText, value);
    }

    /// <summary>Detail of the last failure, empty when there is none.</summary>
    public string ErrorDetail => _errorDetail;

    /// <summary>True when the last operation failed.</summary>
    public bool HasError
    {
        get => _hasError;
        private set => SetProperty(ref _hasError, value);
    }

    /// <summary>Download progress between 0 and 100, or -1 for an unknown size.</summary>
    public double ProgressPercent
    {
        get => _progressPercent;
        private set
        {
            if (SetProperty(ref _progressPercent, Math.Max(0, value)))
            {
                OnPropertyChanged(nameof(ProgressPercentText));
            }
        }
    }

    /// <summary>Formatted download progress.</summary>
    public string ProgressPercentText => Math.Round(Math.Max(0, ProgressPercent), 0) + " %";

    /// <summary>True while the size of the download is unknown.</summary>
    public bool IsIndeterminate
    {
        get => _isIndeterminate;
        private set => SetProperty(ref _isIndeterminate, value);
    }

    /// <summary>True while the update check is running.</summary>
    public bool IsChecking
    {
        get => _isChecking;
        private set
        {
            if (SetProperty(ref _isChecking, value))
            {
                OnPropertyChanged(nameof(IsBusy));
                CheckForUpdatesCommand.RaiseCanExecuteChanged();
            }
        }
    }

    /// <summary>True when a newer release was found.</summary>
    public bool HasUpdate
    {
        get => _hasUpdate;
        private set
        {
            if (SetProperty(ref _hasUpdate, value))
            {
                OnPropertyChanged(nameof(IsBusy));
                UpdateNowCommand.RaiseCanExecuteChanged();
                SkipVersionCommand.RaiseCanExecuteChanged();
            }
        }
    }

    /// <summary>True while the installer is being downloaded.</summary>
    public bool IsDownloading
    {
        get => _isDownloading;
        private set
        {
            if (SetProperty(ref _isDownloading, value))
            {
                OnPropertyChanged(nameof(IsBusy));
                UpdateNowCommand.RaiseCanExecuteChanged();
                CancelCommand.RaiseCanExecuteChanged();
            }
        }
    }

    /// <summary>True while any background operation is active.</summary>
    public bool IsBusy => IsChecking || IsDownloading;

    /// <summary>Version text of the newer release.</summary>
    public string AvailableVersion => _manifest?.Version ?? string.Empty;

    /// <summary>Notes of the newer release.</summary>
    public string ReleaseNotes => _manifest?.ReleaseNotes ?? string.Empty;

    /// <summary>Checks for a newer release and reports the state.</summary>
    public async Task CheckForUpdatesAsync()
    {
        if (IsBusy)
        {
            return;
        }

        HasError = false;
        HasUpdate = false;
        StatusText = "Checking for updates…";
        IsChecking = true;

        UpdateManifest? manifest = await _updates.CheckForUpdatesAsync().ConfigureAwait(true);

        UpdateSettings updates = _settings.Settings.Updates;
        updates.LastCheckUtc = DateTime.UtcNow;
        await _settings.SaveAsync().ConfigureAwait(true);

        IsChecking = false;

        if (manifest is null)
        {
            StatusText = "HyperPDF is up to date. You are running version " + _updates.CurrentVersion + ".";
        }
        else
        {
            ApplyManifest(manifest);
            StatusText = $"HyperPDF {manifest.Version} is available. Current version: {_updates.CurrentVersion}";
        }
    }

    /// <summary>Downloads the installer and launches it; HyperPDF closes itself during the install.</summary>
    public async Task RunUpdateNowAsync()
    {
        UpdateManifest? manifest = _manifest;
        if (manifest is null || IsDownloading)
        {
            return;
        }

        HasError = false;
        IsDownloading = true;
        StatusText = "Downloading HyperPDF " + manifest.Version + "…";
        IsIndeterminate = true;
        _downloadCancellation = new CancellationTokenSource();

        try
        {
            var progress = new Progress<double>(OnDownloadProgress);
            string path = await _updates.DownloadAsync(manifest, progress, _downloadCancellation.Token).ConfigureAwait(true);

            IsDownloading = false;
            StatusText = "Launching the update. HyperPDF closes while the installer runs.";
            OnCloseRequested();
            _updates.LaunchInstaller(path);
            Application.Current.Shutdown();
        }
        catch (OperationCanceledException)
        {
            IsDownloading = false;
            StatusText = "Download cancelled. You can try again later.";
        }
        catch (Exception ex)
        {
            if (IsDownloading)
            {
                IsDownloading = false;
                StatusText = "The update could not be completed.";
                _errorDetail = ex.Message;
                OnPropertyChanged(nameof(ErrorDetail));
                HasError = true;
            }
        }
    }

    /// <summary>Applies a manifest that was found outside of this dialog (silent startup check).</summary>
    public void ApplyManifest(UpdateManifest manifest)
    {
        _manifest = manifest;
        OnPropertyChanged(nameof(Manifest));
        OnPropertyChanged(nameof(AvailableVersion));
        OnPropertyChanged(nameof(ReleaseNotes));
        HasUpdate = true;
    }

    /// <summary>Remembers the version as skipped for the silent startup check.</summary>
    public void SkipVersion()
    {
        if (_manifest is not null)
        {
            _settings.Settings.Updates.SkippedVersion = _manifest.Version;
            _ = _settings.SaveAsync();
        }

        OnCloseRequested();
    }

    /// <summary>Aborts the download or closes the dialog.</summary>
    public void Cancel()
    {
        if (IsDownloading)
        {
            _downloadCancellation?.Cancel();
            return;
        }

        OnCloseRequested();
    }

    private void OnDownloadProgress(double value)
    {
        if (value >= 0)
        {
            IsIndeterminate = false;
            ProgressPercent = value;
        }
    }

    private void OnCloseRequested() => CloseRequested?.Invoke(this, EventArgs.Empty);
}
