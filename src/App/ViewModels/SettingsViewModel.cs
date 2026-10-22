// Author: Anwarul Haque (@HYPERMX7) | Contact: 7739399007
using HyperPDF.Models;
using HyperPDF.Services;

namespace HyperPDF.ViewModels;

/// <summary>View model of the settings dialog.</summary>
public sealed class SettingsViewModel : ViewModelBase
{
    private readonly AppServices _services;

    /// <summary>Creates the view model.</summary>
    public SettingsViewModel(AppServices services)
    {
        _services = services;
        AppSettings settings = services.Settings.Settings;

        Theme = settings.Theme;
        ViewMode = settings.ViewMode;
        RestoreLastPosition = settings.RestoreLastPosition;
        ShowThumbnails = settings.ShowThumbnails;
        ShowStatusBar = settings.ShowStatusBar;

        RemoveRecentCommand = new RelayCommand(parameter =>
        {
            if (parameter is RecentFileEntry entry)
            {
                _services.RecentFiles.Remove(entry.FilePath);
                OnPropertyChanged(nameof(RecentFiles));
            }
        });

        ClearRecentCommand = new RelayCommand(() =>
        {
            if (_services.Dialogs.Confirm("Remove all documents from the recent files list?"))
            {
                _services.RecentFiles.Clear();
                OnPropertyChanged(nameof(RecentFiles));
            }
        });

        OpenDefaultAppsCommand = new RelayCommand(() => _services.Shell.OpenDefaultAppsSettings());

        _services.RecentFiles.Changed += (_, _) => OnPropertyChanged(nameof(RecentFiles));
    }

    /// <summary>Selected colour theme.</summary>
    public AppTheme Theme
    {
        get => _services.Settings.Settings.Theme;
        set
        {
            if (_services.Settings.Settings.Theme == value)
            {
                return;
            }

            _services.Settings.Settings.Theme = value;
            _services.Theme.Apply(value);
            OnPropertyChanged();
        }
    }

    /// <summary>Selected page layout.</summary>
    public ViewMode ViewMode
    {
        get => _services.Settings.Settings.ViewMode;
        set
        {
            if (_services.Settings.Settings.ViewMode == value)
            {
                return;
            }

            _services.Settings.Settings.ViewMode = value;
            OnPropertyChanged();
        }
    }

    /// <summary>True when the reading position is restored.</summary>
    public bool RestoreLastPosition
    {
        get => _services.Settings.Settings.RestoreLastPosition;
        set
        {
            _services.Settings.Settings.RestoreLastPosition = value;
            OnPropertyChanged();
        }
    }

    /// <summary>True when the thumbnail sidebar is shown.</summary>
    public bool ShowThumbnails
    {
        get => _services.Settings.Settings.ShowThumbnails;
        set
        {
            _services.Settings.Settings.ShowThumbnails = value;
            OnPropertyChanged();
        }
    }

    /// <summary>True when the status bar is shown.</summary>
    public bool ShowStatusBar
    {
        get => _services.Settings.Settings.ShowStatusBar;
        set
        {
            _services.Settings.Settings.ShowStatusBar = value;
            OnPropertyChanged();
        }
    }

    /// <summary>Recently opened documents.</summary>
    public IReadOnlyList<RecentFileEntry> RecentFiles => _services.RecentFiles.Items;

    /// <summary>Command that removes a single recent document.</summary>
    public RelayCommand RemoveRecentCommand { get; }

    /// <summary>Command that clears the recent documents list.</summary>
    public RelayCommand ClearRecentCommand { get; }

    /// <summary>Command that opens the Windows default application settings.</summary>
    public RelayCommand OpenDefaultAppsCommand { get; }

    /// <summary>Text that explains how HyperPDF becomes the default PDF reader.</summary>
    public string DefaultAppDescription =>
        "Windows decides which application opens a PDF file. " +
        "Open the default application settings and choose HyperPDF for the .pdf file type.";
}
