// Author: Anwarul Haque (@HYPERMX7) | Contact: 7739399007
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using HyperPDF.Models;
using HyperPDF.Services;
using HyperPDF.Services.Pdfium;

namespace HyperPDF.ViewModels;

/// <summary>
/// View model of a single open document. It owns the native PDFium document, the page list,
/// the zoom and rotation state, the thumbnail previews and the search state.
/// </summary>
public sealed class DocumentViewModel : ViewModelBase, IDisposable
{
    private const double BaseDpi = 96.0;
    private const int MaxCachedRenders = 16;
    private const int MaxThumbnails = 2000;
    private const int ThumbnailWidth = 132;

    private readonly ISettingsService _settingsService;
    private readonly IPrintService _printService;
    private readonly IDialogService _dialogService;
    private readonly PdfDocument _document;
    private readonly Dictionary<(int Page, int Width, int Height), BitmapSource> _renderCache = new();
    private readonly object _renderLock = new();

    private int _currentPageIndex;
    private double _zoomPercent = 100;
    private int _rotation;
    private ViewMode _viewMode = ViewMode.Continuous;
    private bool _isLoading = true;
    private string _searchTerm = string.Empty;
    private IReadOnlyList<TextSearchMatch> _searchResults = Array.Empty<TextSearchMatch>();
    private int _currentMatchIndex = -1;
    private bool _isSearching;
    private int _renderVersion;

    /// <summary>Creates the view model for an already opened document.</summary>
    public DocumentViewModel(
        PdfDocument document,
        ISettingsService settingsService,
        IPrintService printService,
        IDialogService dialogService)
    {
        _document = document ?? throw new ArgumentNullException(nameof(document));
        _settingsService = settingsService;
        _printService = printService;
        _dialogService = dialogService;

        FilePath = document.FilePath;
        FileName = document.FileName;

        for (int index = 0; index < document.PageCount; index++)
        {
            var (width, height) = document.GetPageSize(index);
            Pages.Add(new PageViewModel(index, width, height));
        }

        NextPageCommand = new RelayCommand(() => GoToPage(CurrentPageIndex + 1), () => CanGoToPage(CurrentPageIndex + 1));
        PreviousPageCommand = new RelayCommand(() => GoToPage(CurrentPageIndex - 1), () => CanGoToPage(CurrentPageIndex - 1));
        FirstPageCommand = new RelayCommand(() => GoToPage(0), () => CurrentPageIndex != 0);
        LastPageCommand = new RelayCommand(() => GoToPage(PageCount - 1), () => CurrentPageIndex != PageCount - 1);
        ZoomInCommand = new RelayCommand(() => ZoomPercent = Math.Min(400, Math.Round(ZoomPercent * 1.25, 2)));
        ZoomOutCommand = new RelayCommand(() => ZoomPercent = Math.Max(10, Math.Round(ZoomPercent / 1.25, 2)));
        ZoomResetCommand = new RelayCommand(() => ZoomPercent = 100);
        FitPageCommand = new RelayCommand(() => ZoomMode = Models.ZoomMode.FitPage);
        FitWidthCommand = new RelayCommand(() => ZoomMode = Models.ZoomMode.FitWidth);
        RotateClockwiseCommand = new RelayCommand(() => Rotation = (Rotation + 90) % 360);
        RotateCounterClockwiseCommand = new RelayCommand(() => Rotation = (Rotation + 270) % 360);
        FindNextCommand = new RelayCommand(FindNext, () => HasSearchResults);
        FindPreviousCommand = new RelayCommand(FindPrevious, () => HasSearchResults);
        ClearSearchCommand = new RelayCommand(ClearSearch, () => HasSearchResults);
        CopyPageTextCommand = new AsyncRelayCommand(CopyPageTextAsync);
        PrintCommand = new AsyncRelayCommand(PrintAsync);
        ShowInfoCommand = new AsyncRelayCommand(ShowInfoAsync);
    }

    /// <summary>Raised when the workspace should scroll to a specific page.</summary>
    public event EventHandler<int>? ScrollToPageRequested;

    /// <summary>Full path of the document.</summary>
    public string FilePath { get; }

    /// <summary>File name of the document.</summary>
    public string FileName { get; }

    /// <summary>Title shown on the document tab.</summary>
    public string Title => FileName;

    /// <summary>The native PDFium document.</summary>
    public PdfDocument Document => _document;

    /// <summary>All pages of the document.</summary>
    public ObservableCollection<PageViewModel> Pages { get; } = new();

    /// <summary>Thumbnail previews of the document.</summary>
    public ObservableCollection<ThumbnailViewModel> Thumbnails { get; } = new();

    /// <summary>Number of pages.</summary>
    public int PageCount => _document.PageCount;

    /// <summary>True when the document is protected by an encryption handler.</summary>
    public bool IsEncrypted => _document.IsEncrypted;

    /// <summary>Zero based index of the current page.</summary>
    public int CurrentPageIndex
    {
        get => _currentPageIndex;
        set
        {
            int clamped = Math.Clamp(value, 0, Math.Max(0, PageCount - 1));
            if (SetProperty(ref _currentPageIndex, clamped))
            {
                OnPropertyChanged(nameof(CurrentPageNumber));
                OnPropertyChanged(nameof(CurrentPageLabel));
                RefreshCommandStates();
            }
        }
    }

    /// <summary>One based number of the current page.</summary>
    public int CurrentPageNumber => CurrentPageIndex + 1;

    /// <summary>Page label of the current page, when the document defines one.</summary>
    public string CurrentPageLabel => _document.GetPageLabel(CurrentPageIndex);

    /// <summary>Zoom percentage of the workspace.</summary>
    public double ZoomPercent
    {
        get => _zoomPercent;
        set
        {
            double clamped = Math.Clamp(Math.Round(value, 2), 10, 400);
            if (SetProperty(ref _zoomPercent, clamped))
            {
                OnPropertyChanged(nameof(ZoomText));
                OnPropertyChanged(nameof(Scale));
                ApplyMetrics();
            }
        }
    }

    /// <summary>Zoom percentage formatted for the status bar.</summary>
    public string ZoomText => $"{Math.Round(ZoomPercent):0} %";

    /// <summary>Zoom strategy of the workspace.</summary>
    public ZoomMode ZoomMode { get; private set; } = Models.ZoomMode.FitWidth;

    /// <summary>Conversion factor from PDF points to device independent pixels.</summary>
    public double Scale => ZoomPercent / 100.0 * (BaseDpi / 72.0);

    /// <summary>Rotation of the view in degrees (0, 90, 180 or 270).</summary>
    public int Rotation
    {
        get => _rotation;
        set
        {
            int normalized = ((value % 360) + 360) % 360;
            if (SetProperty(ref _rotation, normalized))
            {
                OnPropertyChanged(nameof(IsRotated));
                ApplyMetrics();
            }
        }
    }

    /// <summary>True when the view is rotated by 90 or 270 degrees.</summary>
    public bool IsRotated => Rotation is 90 or 270;

    /// <summary>Page layout of the workspace.</summary>
    public ViewMode ViewMode
    {
        get => _viewMode;
        set
        {
            if (SetProperty(ref _viewMode, value))
            {
                OnPropertyChanged(nameof(IsContinuous));
            }
        }
    }

    /// <summary>True when the workspace shows all pages in a continuous column.</summary>
    public bool IsContinuous => ViewMode == ViewMode.Continuous;

    /// <summary>True while the document is being opened.</summary>
    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    /// <summary>Incremented whenever cached renderings become invalid.</summary>
    public int RenderVersion
    {
        get => _renderVersion;
        private set => SetProperty(ref _renderVersion, value);
    }

    /// <summary>Current search term.</summary>
    public string SearchTerm
    {
        get => _searchTerm;
        set => SetProperty(ref _searchTerm, value);
    }

    /// <summary>All hits of the last search.</summary>
    public IReadOnlyList<TextSearchMatch> SearchResults
    {
        get => _searchResults;
        private set
        {
            if (SetProperty(ref _searchResults, value))
            {
                OnPropertyChanged(nameof(HasSearchResults));
                OnPropertyChanged(nameof(SearchStatusText));
                RefreshCommandStates();
            }
        }
    }

    /// <summary>True when the last search produced at least one hit.</summary>
    public bool HasSearchResults => _searchResults.Count > 0;

    /// <summary>True while a search is running.</summary>
    public bool IsSearching
    {
        get => _isSearching;
        set => SetProperty(ref _isSearching, value);
    }

    /// <summary>Index of the highlighted search hit.</summary>
    public int CurrentMatchIndex
    {
        get => _currentMatchIndex;
        private set
        {
            if (SetProperty(ref _currentMatchIndex, value))
            {
                OnPropertyChanged(nameof(CurrentMatchText));
            }
        }
    }

    /// <summary>Text shown next to the search box.</summary>
    public string CurrentMatchText => HasSearchResults
        ? $"{CurrentMatchIndex + 1} of {_searchResults.Count}"
        : "0 of 0";

    /// <summary>Status text of the search box.</summary>
    public string SearchStatusText
    {
        get
        {
            if (IsSearching)
            {
                return "Searching...";
            }

            if (string.IsNullOrEmpty(SearchTerm))
            {
                return string.Empty;
            }

            if (_searchResults.Count == 0)
            {
                return "No matches";
            }

            return CurrentMatchText;
        }
    }

    /// <summary>Command that navigates to the next page.</summary>
    public RelayCommand NextPageCommand { get; }

    /// <summary>Command that navigates to the previous page.</summary>
    public RelayCommand PreviousPageCommand { get; }

    /// <summary>Command that navigates to the first page.</summary>
    public RelayCommand FirstPageCommand { get; }

    /// <summary>Command that navigates to the last page.</summary>
    public RelayCommand LastPageCommand { get; }

    /// <summary>Command that increases the zoom level.</summary>
    public RelayCommand ZoomInCommand { get; }

    /// <summary>Command that decreases the zoom level.</summary>
    public RelayCommand ZoomOutCommand { get; }

    /// <summary>Command that resets the zoom level to 100 %.</summary>
    public RelayCommand ZoomResetCommand { get; }

    /// <summary>Command that fits the whole page into the viewport.</summary>
    public RelayCommand FitPageCommand { get; }

    /// <summary>Command that fits the page width into the viewport.</summary>
    public RelayCommand FitWidthCommand { get; }

    /// <summary>Command that rotates the view clockwise.</summary>
    public RelayCommand RotateClockwiseCommand { get; }

    /// <summary>Command that rotates the view counter clockwise.</summary>
    public RelayCommand RotateCounterClockwiseCommand { get; }

    /// <summary>Command that jumps to the next search hit.</summary>
    public RelayCommand FindNextCommand { get; }

    /// <summary>Command that jumps to the previous search hit.</summary>
    public RelayCommand FindPreviousCommand { get; }

    /// <summary>Command that removes all search hits.</summary>
    public RelayCommand ClearSearchCommand { get; }

    /// <summary>Command that copies the text of the current page to the clipboard.</summary>
    public AsyncRelayCommand CopyPageTextCommand { get; }

    /// <summary>Command that prints the document.</summary>
    public AsyncRelayCommand PrintCommand { get; }

    /// <summary>Command that shows the document properties.</summary>
    public AsyncRelayCommand ShowInfoCommand { get; }

    /// <summary>Applies the zoom strategy for the given viewport size.</summary>
    public void ApplyZoomToViewport(double viewportWidth, double viewportHeight)
    {
        if (PageCount == 0 || viewportWidth <= 0 || viewportHeight <= 0)
        {
            return;
        }

        var (pageWidth, pageHeight) = _document.GetPageSize(CurrentPageIndex);
        if (pageWidth <= 0 || pageHeight <= 0)
        {
            return;
        }

        double scale = ZoomMode switch
        {
            Models.ZoomMode.FitPage => Math.Min(viewportWidth / pageWidth, viewportHeight / pageHeight),
            Models.ZoomMode.FitWidth => viewportWidth / pageWidth,
            _ => Scale
        };

        if (scale <= 0 || double.IsNaN(scale) || double.IsInfinity(scale))
        {
            return;
        }

        ZoomPercent = Math.Clamp(Math.Round(scale * 100.0 * (72.0 / BaseDpi), 2), 10, 400);
    }

    /// <summary>Sets the zoom strategy.</summary>
    public void SetZoomMode(ZoomMode mode) => ZoomMode = mode;

    /// <summary>Navigates to a one based page number.</summary>
    public void GoToPage(int pageNumber) => GoToPageIndex(pageNumber - 1);

    /// <summary>Navigates to a zero based page index.</summary>
    public void GoToPageIndex(int pageIndex)
    {
        int clamped = Math.Clamp(pageIndex, 0, Math.Max(0, PageCount - 1));
        CurrentPageIndex = clamped;
        ScrollToPageRequested?.Invoke(this, clamped);
    }

    /// <summary>Called by the workspace when the user scrolls to another page.</summary>
    public void SetCurrentPage(int pageIndex) => CurrentPageIndex = pageIndex;

    private bool CanGoToPage(int pageIndex) => pageIndex >= 0 && pageIndex < PageCount;

    private void RefreshCommandStates()
    {
        NextPageCommand.RaiseCanExecuteChanged();
        PreviousPageCommand.RaiseCanExecuteChanged();
        FirstPageCommand.RaiseCanExecuteChanged();
        LastPageCommand.RaiseCanExecuteChanged();
        FindNextCommand.RaiseCanExecuteChanged();
        FindPreviousCommand.RaiseCanExecuteChanged();
        ClearSearchCommand.RaiseCanExecuteChanged();
    }

    private void ApplyMetrics()
    {
        double scale = Scale;
        bool swap = IsRotated;
        foreach (PageViewModel page in Pages)
        {
            page.UpdateMetrics(scale, swap);
        }

        InvalidateRenderCache();
    }

    /// <summary>Drops every cached rendering so the pages are rendered again.</summary>
    public void InvalidateRenderCache()
    {
        lock (_renderLock)
        {
            _renderCache.Clear();
        }

        RenderVersion++;
    }

    /// <summary>
    /// Renders a page into a bitmap. Cancelled requests never publish their result, which
    /// prevents a stale rendering from replacing a newer one.
    /// </summary>
    public async Task<BitmapSource?> RenderPageAsync(int pageIndex, int widthPx, int heightPx, CancellationToken cancellationToken)
    {
        if (pageIndex < 0 || pageIndex >= PageCount || widthPx <= 0 || heightPx <= 0)
        {
            return null;
        }

        var key = (pageIndex, widthPx, heightPx);
        lock (_renderLock)
        {
            if (_renderCache.TryGetValue(key, out BitmapSource? cached))
            {
                return cached;
            }
        }

        BitmapSource? result = await Task.Run(
            () => RenderPageCore(pageIndex, widthPx, heightPx, cancellationToken),
            cancellationToken).ConfigureAwait(true);

        if (result is null || cancellationToken.IsCancellationRequested)
        {
            return null;
        }

        lock (_renderLock)
        {
            if (_renderCache.Count >= MaxCachedRenders)
            {
                _renderCache.Clear();
            }

            _renderCache[key] = result;
        }

        return result;
    }

    private BitmapSource? RenderPageCore(int pageIndex, int widthPx, int heightPx, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return null;
        }

        RenderedPage rendered;
        try
        {
            rendered = PdfiumRenderer.Render(_document, pageIndex, widthPx, heightPx);
        }
        catch (PdfOpenException)
        {
            return null;
        }

        if (cancellationToken.IsCancellationRequested)
        {
            return null;
        }

        ApplyHighlights(rendered, pageIndex);
        return rendered.ToBitmapSource();
    }

    private void ApplyHighlights(RenderedPage rendered, int pageIndex)
    {
        if (_searchResults.Count == 0)
        {
            return;
        }

        double scale = widthScale(rendered);
        double pageHeight = rendered.PageHeightPoints;

        foreach (TextSearchMatch match in _searchResults)
        {
            if (match.PageIndex != pageIndex)
            {
                continue;
            }

            foreach (PdfRect rect in match.Rectangles)
            {
                double left = rect.Left * scale;
                double right = rect.Right * scale;
                double top = (pageHeight - rect.Top) * scale;
                double bottom = (pageHeight - rect.Bottom) * scale;

                rendered.BlendHighlight(
                    (int)Math.Round(left),
                    (int)Math.Round(top),
                    (int)Math.Round(right),
                    (int)Math.Round(bottom),
                    255,
                    214,
                    0,
                    0.45);
            }
        }
    }

    private double widthScale(RenderedPage rendered)
    {
        if (rendered.PageWidthPoints <= 0)
        {
            return 1;
        }

        return rendered.Width / rendered.PageWidthPoints;
    }

    /// <summary>Generates the thumbnail previews in the background.</summary>
    public async Task LoadThumbnailsAsync(CancellationToken cancellationToken)
    {
        int count = Math.Min(PageCount, MaxThumbnails);
        for (int index = 0; index < count; index++)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            var thumbnail = new ThumbnailViewModel(index, _document.GetPageLabel(index));
            Thumbnails.Add(thumbnail);

            try
            {
                var (width, height) = _document.GetPageSize(index);
                double ratio = height <= 0 ? 1.414 : height / width;
                int pixelWidth = ThumbnailWidth;
                int pixelHeight = Math.Max(1, (int)Math.Round(ThumbnailWidth * ratio));

                RenderedPage rendered = await Task.Run(
                    () => PdfiumRenderer.Render(_document, index, pixelWidth, pixelHeight),
                    cancellationToken).ConfigureAwait(true);

                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                thumbnail.Image = rendered.ToBitmapSource();
            }
            catch (PdfOpenException)
            {
                // A single broken page must not stop the whole sidebar.
            }
        }
    }

    /// <summary>Runs a search over the whole document.</summary>
    public async Task FindAsync(string term, bool matchCase = false)
    {
        SearchTerm = term ?? string.Empty;
        CurrentMatchIndex = -1;

        if (string.IsNullOrWhiteSpace(SearchTerm))
        {
            SearchResults = Array.Empty<TextSearchMatch>();
            InvalidateRenderCache();
            return;
        }

        IsSearching = true;
        OnPropertyChanged(nameof(SearchStatusText));
        try
        {
            IReadOnlyList<TextSearchMatch> results = await Task.Run(
                () => PdfiumTextService.Search(_document, SearchTerm, matchCase),
                CancellationToken.None).ConfigureAwait(true);

            SearchResults = results;
            InvalidateRenderCache();

            if (results.Count > 0)
            {
                CurrentMatchIndex = 0;
                GoToPageIndex(results[0].PageIndex);
            }
        }
        finally
        {
            IsSearching = false;
            OnPropertyChanged(nameof(SearchStatusText));
        }
    }

    /// <summary>Selects the next search hit.</summary>
    public void FindNext()
    {
        if (!HasSearchResults)
        {
            return;
        }

        CurrentMatchIndex = (CurrentMatchIndex + 1) % _searchResults.Count;
        GoToPageIndex(_searchResults[CurrentMatchIndex].PageIndex);
        OnPropertyChanged(nameof(SearchStatusText));
    }

    /// <summary>Selects the previous search hit.</summary>
    public void FindPrevious()
    {
        if (!HasSearchResults)
        {
            return;
        }

        CurrentMatchIndex = (_currentMatchIndex - 1 + _searchResults.Count) % _searchResults.Count;
        GoToPageIndex(_searchResults[CurrentMatchIndex].PageIndex);
        OnPropertyChanged(nameof(SearchStatusText));
    }

    /// <summary>Removes the search results and the highlights.</summary>
    public void ClearSearch()
    {
        SearchTerm = string.Empty;
        SearchResults = Array.Empty<TextSearchMatch>();
        CurrentMatchIndex = -1;
        InvalidateRenderCache();
        OnPropertyChanged(nameof(SearchStatusText));
    }

    /// <summary>Copies the text layer of the current page to the clipboard.</summary>
    public async Task CopyPageTextAsync()
    {
        try
        {
            string text = await Task.Run(
                () => PdfiumTextService.ExtractPageText(_document, CurrentPageIndex),
                CancellationToken.None).ConfigureAwait(true);

            if (string.IsNullOrWhiteSpace(text))
            {
                _dialogService.ShowInformation(
                    "This page does not contain any extractable text.",
                    "Scanned pages are images and therefore have no text layer.");
                return;
            }

            Clipboard.SetText(text);
        }
        catch (Exception ex) when (ex is System.Runtime.InteropServices.ExternalException)
        {
            _dialogService.ShowError("The text could not be copied to the clipboard.");
        }
    }

    /// <summary>Prints the document.</summary>
    public async Task PrintAsync()
    {
        bool printed = await Task.Run(() =>
        {
            bool result = _printService.Print(_document, FileName, out string? error);
            if (!result && !string.IsNullOrEmpty(error))
            {
                _dialogService.ShowError("The document could not be printed.", error);
            }

            return result;
        }).ConfigureAwait(true);

        if (printed)
        {
            // Nothing else to do, the print job was accepted by the spooler.
        }
    }

    /// <summary>Collects the document properties for the properties dialog.</summary>
    public async Task<DocumentInfo> GetDocumentInfoAsync()
    {
        return await Task.Run(() =>
        {
            var info = PdfMetadataScanner.Scan(FilePath);
            info.PageCount = PageCount;
            info.PdfVersion = _document.PdfVersion > 0
                ? _document.PdfVersion.ToString(System.Globalization.CultureInfo.InvariantCulture)
                : "unknown";
            info.FileId = _document.GetFileIdentifier();
            info.IsEncrypted = _document.IsEncrypted;
            info.SecurityHandlerRevision = _document.GetSecurityHandlerRevision();
            return info;
        }).ConfigureAwait(true);
    }

    /// <summary>Shows the document properties dialog.</summary>
    public async Task ShowInfoAsync()
    {
        DocumentInfo info = await GetDocumentInfoAsync().ConfigureAwait(true);
        var viewModel = new DocumentInfoViewModel(info);
        await Views.DocumentInfoDialog.ShowAsync(viewModel).ConfigureAwait(true);
    }

    /// <summary>Persists the reading position of the document.</summary>
    public void SaveSessionState()
    {
        if (!_settingsService.Settings.RestoreLastPosition)
        {
            return;
        }

        _settingsService.Settings.DocumentStates[FilePath] = new DocumentSessionState
        {
            LastPageIndex = CurrentPageIndex,
            ZoomPercent = ZoomPercent,
            ViewMode = ViewMode
        };
    }

    /// <summary>Restores a previously stored reading position.</summary>
    public void RestoreSessionState()
    {
        if (!_settingsService.Settings.RestoreLastPosition)
        {
            return;
        }

        if (!_settingsService.Settings.DocumentStates.TryGetValue(FilePath, out DocumentSessionState? state))
        {
            return;
        }

        ViewMode = state.ViewMode;
        ZoomPercent = state.ZoomPercent > 0 ? state.ZoomPercent : 100;
        CurrentPageIndex = Math.Clamp(state.LastPageIndex, 0, Math.Max(0, PageCount - 1));
    }

    /// <inheritdoc />
    public void Dispose()
    {
        lock (_renderLock)
        {
            _renderCache.Clear();
        }

        Pages.Clear();
        Thumbnails.Clear();
        _document.Dispose();
    }
}
