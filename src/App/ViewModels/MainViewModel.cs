// Author: Anwarul Haque (@HYPERMX7) | Contact: 7739399007
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using HyperPDF.Models;
using HyperPDF.Services;

namespace HyperPDF.ViewModels;

/// <summary>View model of the main window.</summary>
public sealed class MainViewModel : ViewModelBase
{
    private readonly ISettingsService _settingsService;
    private readonly IRecentFilesService _recentFilesService;
    private readonly IDialogService _dialogService;
    private readonly IFileDialogService _fileDialogService;
    private readonly IShellService _shellService;
    private readonly IThemeService _themeService;
    private readonly IDocumentService _documentService;

    private DocumentViewModel? _selectedDocument;
    private bool _isSidebarVisible = true;
    private bool _isFullScreen;
    private string _statusMessage = string.Empty;

    /// <summary>Creates the main view model.</summary>
    public MainViewModel(
        ISettingsService settingsService,
        IRecentFilesService recentFilesService,
        IDialogService dialogService,
        IFileDialogService fileDialogService,
        IShellService shellService,
        IThemeService themeService,
        IDocumentService documentService)
    {
        _settingsService = settingsService;
        _recentFilesService = recentFilesService;
        _dialogService = dialogService;
        _fileDialogService = fileDialogService;
        _shellService = shellService;
        _themeService = themeService;
        _documentService = documentService;

        _isSidebarVisible = settingsService.Settings.ShowThumbnails;

        OpenCommand = new AsyncRelayCommand(OpenDocumentAsync);
        OpenFileCommand = new AsyncRelayCommand(OpenFileAsync);
        OpenRecentCommand = new AsyncRelayCommand(OpenRecentAsync);
        RemoveRecentCommand = new RelayCommand(RemoveRecent);
        ClearRecentCommand = new RelayCommand(ClearRecent);
        ShowInFolderCommand = new RelayCommand(ShowInFolder);
        CloseDocumentCommand = new RelayCommand(CloseDocument);
        CloseCurrentDocumentCommand = new RelayCommand(CloseCurrentDocument);
        CloseAllDocumentsCommand = new RelayCommand(CloseAllDocuments, () => Documents.Count > 0);
        NextTabCommand = new RelayCommand(SelectNextTab, () => Documents.Count > 1);
        PreviousTabCommand = new RelayCommand(SelectPreviousTab, () => Documents.Count > 1);
        ToggleSidebarCommand = new RelayCommand(ToggleSidebar);
        ToggleFullScreenCommand = new RelayCommand(ToggleFullScreen);
        ToggleThemeCommand = new RelayCommand(ToggleTheme);
        ShowAboutCommand = new RelayCommand(ShowAbout);
        ShowSettingsCommand = new RelayCommand(ShowSettings);
        PrintCommand = new AsyncRelayCommand(PrintCurrentDocumentAsync, () => SelectedDocument is not null);
        FindCommand = new RelayCommand(RequestFind);
        GoToPageCommand = new RelayCommand(GoToPage);
        ZoomInCommand = new RelayCommand(() => SelectedDocument?.ZoomInCommand.Execute(null));
        ZoomOutCommand = new RelayCommand(() => SelectedDocument?.ZoomOutCommand.Execute(null));
        ZoomResetCommand = new RelayCommand(() => SelectedDocument?.ZoomResetCommand.Execute(null));
        FitPageCommand = new RelayCommand(() => SelectedDocument?.FitPageCommand.Execute(null));
        FitWidthCommand = new RelayCommand(() => SelectedDocument?.FitWidthCommand.Execute(null));
        FirstPageCommand = new RelayCommand(() => SelectedDocument?.FirstPageCommand.Execute(null));
        PreviousPageCommand = new RelayCommand(() => SelectedDocument?.PreviousPageCommand.Execute(null));
        NextPageCommand = new RelayCommand(() => SelectedDocument?.NextPageCommand.Execute(null));
        LastPageCommand = new RelayCommand(() => SelectedDocument?.LastPageCommand.Execute(null));
        RotateClockwiseCommand = new RelayCommand(() => SelectedDocument?.RotateClockwiseCommand.Execute(null));
        RotateCounterClockwiseCommand = new RelayCommand(() => SelectedDocument?.RotateCounterClockwiseCommand.Execute(null));

        _recentFilesService.Changed += RecentFilesChanged;
    }

    /// <summary>Raised when the workspace should focus the search box.</summary>
    public event EventHandler? FindRequested;

    /// <summary>Raised when the theme changed and the window chrome has to be refreshed.</summary>
    public event EventHandler? ThemeChanged;

    /// <summary>All open documents.</summary>
    public ObservableCollection<DocumentViewModel> Documents { get; } = new();

    /// <summary>The document that is currently displayed.</summary>
    public DocumentViewModel? SelectedDocument
    {
        get => _selectedDocument;
        set
        {
            if (SetProperty(ref _selectedDocument, value))
            {
                OnPropertyChanged(nameof(IsWelcomeVisible));
                OnPropertyChanged(nameof(WindowTitle));
                RefreshCommandStates();
            }
        }
    }

    /// <summary>True while no document is open and the welcome screen is shown.</summary>
    public bool IsWelcomeVisible => Documents.Count == 0;

    /// <summary>Title of the main window.</summary>
    public string WindowTitle => SelectedDocument is null
        ? BrandInfo.ApplicationTitle
        : BrandInfo.DocumentTitle(SelectedDocument.FileName);

    /// <summary>True when the thumbnail sidebar is visible.</summary>
    public bool IsSidebarVisible
    {
        get => _isSidebarVisible;
        set
        {
            if (SetProperty(ref _isSidebarVisible, value))
            {
                _settingsService.Settings.ShowThumbnails = value;
            }
        }
    }

    /// <summary>True when the window is in full screen mode.</summary>
    public bool IsFullScreen
    {
        get => _isFullScreen;
        set
        {
            if (SetProperty(ref _isFullScreen, value))
            {
                OnPropertyChanged(nameof(WindowTitle));
            }
        }
    }

    /// <summary>Text shown in the status bar next to the page information.</summary>
    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    /// <summary>Recently opened documents.</summary>
    public IReadOnlyList<RecentFileEntry> RecentFiles => _recentFilesService.Items;

    /// <summary>True when there is no recent document to show.</summary>
    public bool HasNoRecentFiles => _recentFilesService.Items.Count == 0;

    /// <summary>Application name shown in the welcome screen.</summary>
    public string WelcomeTitle => BrandInfo.ApplicationName;

    /// <summary>Short description shown in the welcome screen.</summary>
    public string WelcomeSubtitle => "A fast, focused PDF reader for Windows.";

    /// <summary>Attribution shown in the welcome screen and the about dialog.</summary>
    public string Attribution => BrandInfo.Attribution;

    /// <summary>Command that opens the file picker.</summary>
    public AsyncRelayCommand OpenCommand { get; }

    /// <summary>Command that opens a file given as command parameter.</summary>
    public AsyncRelayCommand OpenFileCommand { get; }

    /// <summary>Command that opens a recent document.</summary>
    public AsyncRelayCommand OpenRecentCommand { get; }

    /// <summary>Command that removes a single entry from the recent documents list.</summary>
    public RelayCommand RemoveRecentCommand { get; }

    /// <summary>Command that clears the recent documents list.</summary>
    public RelayCommand ClearRecentCommand { get; }

    /// <summary>Command that reveals a recent document in Windows Explorer.</summary>
    public RelayCommand ShowInFolderCommand { get; }

    /// <summary>Command that closes a document given as command parameter.</summary>
    public RelayCommand CloseDocumentCommand { get; }

    /// <summary>Command that closes the current document.</summary>
    public RelayCommand CloseCurrentDocumentCommand { get; }

    /// <summary>Command that closes every open document.</summary>
    public RelayCommand CloseAllDocumentsCommand { get; }

    /// <summary>Command that activates the next document tab.</summary>
    public RelayCommand NextTabCommand { get; }

    /// <summary>Command that activates the previous document tab.</summary>
    public RelayCommand PreviousTabCommand { get; }

    /// <summary>Command that shows or hides the thumbnail sidebar.</summary>
    public RelayCommand ToggleSidebarCommand { get; }

    /// <summary>Command that switches between windowed and full screen mode.</summary>
    public RelayCommand ToggleFullScreenCommand { get; }

    /// <summary>Command that switches between the light and the dark theme.</summary>
    public RelayCommand ToggleThemeCommand { get; }

    /// <summary>Command that shows the about dialog.</summary>
    public RelayCommand ShowAboutCommand { get; }

    /// <summary>Command that shows the settings dialog.</summary>
    public RelayCommand ShowSettingsCommand { get; }

    /// <summary>Command that prints the current document.</summary>
    public AsyncRelayCommand PrintCommand { get; }

    /// <summary>Command that focuses the search box.</summary>
    public RelayCommand FindCommand { get; }

    /// <summary>Command that jumps to a page number given as command parameter.</summary>
    public RelayCommand GoToPageCommand { get; }

    /// <summary>Command that increases the zoom level of the current document.</summary>
    public RelayCommand ZoomInCommand { get; }

    /// <summary>Command that decreases the zoom level of the current document.</summary>
    public RelayCommand ZoomOutCommand { get; }

    /// <summary>Command that resets the zoom level of the current document.</summary>
    public RelayCommand ZoomResetCommand { get; }

    /// <summary>Command that fits the whole page into the viewport.</summary>
    public RelayCommand FitPageCommand { get; }

    /// <summary>Command that fits the page width into the viewport.</summary>
    public RelayCommand FitWidthCommand { get; }

    /// <summary>Command that jumps to the first page of the current document.</summary>
    public RelayCommand FirstPageCommand { get; }

    /// <summary>Command that jumps to the previous page of the current document.</summary>
    public RelayCommand PreviousPageCommand { get; }

    /// <summary>Command that jumps to the next page of the current document.</summary>
    public RelayCommand NextPageCommand { get; }

    /// <summary>Command that jumps to the last page of the current document.</summary>
    public RelayCommand LastPageCommand { get; }

    /// <summary>Command that rotates the current document clockwise.</summary>
    public RelayCommand RotateClockwiseCommand { get; }

    /// <summary>Command that rotates the current document counter clockwise.</summary>
    public RelayCommand RotateCounterClockwiseCommand { get; }

    /// <summary>True when the status bar is visible.</summary>
    public bool ShowStatusBar
    {
        get => _settingsService.Settings.ShowStatusBar;
        set
        {
            if (_settingsService.Settings.ShowStatusBar != value)
            {
                _settingsService.Settings.ShowStatusBar = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>Opens a document with the file picker.</summary>
    public async Task OpenDocumentAsync()
    {
        IReadOnlyList<string> files = _fileDialogService.OpenPdfs();
        foreach (string file in files)
        {
            await OpenFileAsync(file);
        }
    }

    /// <summary>Opens a single document.</summary>
    public async Task OpenFileAsync(object? parameter)
    {
        string? path = parameter as string;
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        DocumentViewModel? document = await _documentService.OpenAsync(path);
        if (document is null)
        {
            return;
        }

        if (Documents.Any(d => string.Equals(d.FilePath, document.FilePath, StringComparison.OrdinalIgnoreCase)))
        {
            // The document is already open, simply activate its tab.
            document.Dispose();
            DocumentViewModel? existing = Documents.First(d => string.Equals(d.FilePath, path, StringComparison.OrdinalIgnoreCase));
            SelectedDocument = existing;
            return;
        }

        Documents.Add(document);
        SelectedDocument = document;
        _recentFilesService.Add(document.FilePath);

        OnPropertyChanged(nameof(IsWelcomeVisible));
        OnPropertyChanged(nameof(WindowTitle));
        RefreshCommandStates();
    }

    private async Task OpenRecentAsync(object? parameter)
    {
        if (parameter is RecentFileEntry entry)
        {
            await OpenFileAsync(entry.FilePath);
        }
    }

    private void RemoveRecent(object? parameter)
    {
        if (parameter is RecentFileEntry entry)
        {
            _recentFilesService.Remove(entry.FilePath);
        }
    }

    private void ClearRecent()
    {
        if (_dialogService.Confirm("Remove all documents from the recent files list?"))
        {
            _recentFilesService.Clear();
        }
    }

    private void ShowInFolder(object? parameter)
    {
        if (parameter is RecentFileEntry entry)
        {
            _shellService.ShowInExplorer(entry.FilePath);
        }
    }

    private void CloseDocument(object? parameter)
    {
        DocumentViewModel? document = parameter as DocumentViewModel ?? SelectedDocument;
        if (document is null)
        {
            return;
        }

        document.SaveSessionState();
        int index = Documents.IndexOf(document);
        Documents.Remove(document);
        document.Dispose();

        if (Documents.Count == 0)
        {
            SelectedDocument = null;
        }
        else if (index >= Documents.Count)
        {
            SelectedDocument = Documents[Documents.Count - 1];
        }

        OnPropertyChanged(nameof(IsWelcomeVisible));
        OnPropertyChanged(nameof(WindowTitle));
        RefreshCommandStates();
    }

    private void CloseCurrentDocument() => CloseDocument(SelectedDocument);

    private void CloseAllDocuments()
    {
        foreach (DocumentViewModel document in Documents.ToArray())
        {
            document.SaveSessionState();
            document.Dispose();
        }

        Documents.Clear();
        SelectedDocument = null;
        OnPropertyChanged(nameof(IsWelcomeVisible));
        OnPropertyChanged(nameof(WindowTitle));
        RefreshCommandStates();
    }

    private void SelectNextTab()
    {
        if (Documents.Count == 0)
        {
            return;
        }

        int index = SelectedDocument is null ? -1 : Documents.IndexOf(SelectedDocument);
        SelectedDocument = Documents[(index + 1) % Documents.Count];
    }

    private void SelectPreviousTab()
    {
        if (Documents.Count == 0)
        {
            return;
        }

        int index = SelectedDocument is null ? -1 : Documents.IndexOf(SelectedDocument);
        SelectedDocument = Documents[(index - 1 + Documents.Count) % Documents.Count];
    }

    private void ToggleSidebar() => IsSidebarVisible = !IsSidebarVisible;

    private void ToggleFullScreen() => IsFullScreen = !IsFullScreen;

    private void ToggleTheme()
    {
        AppTheme target = _themeService.CurrentTheme == AppTheme.Dark ? AppTheme.Light : AppTheme.Dark;
        _themeService.Apply(target);
        _settingsService.Settings.Theme = target;
        ThemeChanged?.Invoke(this, EventArgs.Empty);
    }

    private void ShowAbout() => Views.AboutDialog.Show(BrandInfo.ApplicationName);

    private void ShowSettings() => Views.SettingsDialog.ShowSettings();

    private async Task PrintCurrentDocumentAsync()
    {
        DocumentViewModel? document = SelectedDocument;
        if (document is not null)
        {
            await document.PrintCommand.ExecuteAsync(null);
        }
    }

    private void RequestFind() => FindRequested?.Invoke(this, EventArgs.Empty);

    private void GoToPage(object? parameter)
    {
        if (SelectedDocument is null)
        {
            return;
        }

        switch (parameter)
        {
            case int pageNumber:
                SelectedDocument.GoToPage(pageNumber);
                break;
            case string text when int.TryParse(text, out int parsed):
                SelectedDocument.GoToPage(parsed);
                break;
        }
    }

    private void RecentFilesChanged(object? sender, EventArgs e)
    {
        OnPropertyChanged(nameof(RecentFiles));
        OnPropertyChanged(nameof(HasNoRecentFiles));
    }

    private void RefreshCommandStates()
    {
        CloseAllDocumentsCommand.RaiseCanExecuteChanged();
        NextTabCommand.RaiseCanExecuteChanged();
        PreviousTabCommand.RaiseCanExecuteChanged();
        PrintCommand.RaiseCanExecuteChanged();
    }
}
