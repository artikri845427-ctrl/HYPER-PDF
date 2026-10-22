// Author: Anwarul Haque (@HYPERMX7) | Contact: 7739399007
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using HyperPDF.ViewModels;

namespace HyperPDF.Views;

/// <summary>
/// The workspace of a single document. It hosts the thumbnail sidebar, the find bar and the
/// virtualised list of rendered pages.
/// </summary>
public partial class DocumentView : UserControl
{
    private const double PageSpacing = 16.0;

    private DocumentViewModel? _document;
    private bool _isSyncingSelection;
    private long _lastScrollTick;
    private CancellationTokenSource? _thumbnailCancellation;

    /// <summary>Creates the workspace.</summary>
    public DocumentView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        SizeChanged += OnWorkspaceSizeChanged;
    }

    /// <summary>Shows the find bar and focuses the search box.</summary>
    public void ShowFindBar()
    {
        FindBar.Visibility = Visibility.Visible;
        FindTextBox.Focus();
        FindTextBox.SelectAll();
    }

    /// <summary>Hides the find bar.</summary>
    public void HideFindBar()
    {
        FindBar.Visibility = Visibility.Collapsed;
        _document?.ClearSearch();
    }

    /// <summary>Scrolls the workspace to the given page index.</summary>
    public void ScrollToPage(int pageIndex)
    {
        if (_document is null || pageIndex < 0 || pageIndex >= _document.Pages.Count)
        {
            return;
        }

        double offset = OffsetOfPage(pageIndex);
        PageScrollViewer.ScrollToVerticalOffset(Math.Max(0, offset - 4));
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        _thumbnailCancellation?.Cancel();
        _thumbnailCancellation?.Dispose();
        _thumbnailCancellation = null;

        if (_document is not null)
        {
            _document.ScrollToPageRequested -= OnScrollToPageRequested;
            _document.PropertyChanged -= OnDocumentPropertyChanged;
        }

        _document = e.NewValue as DocumentViewModel;

        if (_document is not null)
        {
            _document.ScrollToPageRequested += OnScrollToPageRequested;
            _document.PropertyChanged += OnDocumentPropertyChanged;

            var cancellation = new CancellationTokenSource();
            _thumbnailCancellation = cancellation;
            _ = LoadThumbnailsAsync(_document, cancellation.Token);
        }
    }

    private static async Task LoadThumbnailsAsync(DocumentViewModel document, CancellationToken cancellationToken)
    {
        try
        {
            await document.LoadThumbnailsAsync(cancellationToken).ConfigureAwait(true);
        }
        catch (Exception ex) when (ex is OperationCanceledException or HyperPDF.Services.Pdfium.PdfOpenException)
        {
            // The document was closed while the thumbnails were still rendering.
        }
    }

    private void OnWorkspaceSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (_document is null)
        {
            return;
        }

        if (_document.ZoomMode is HyperPDF.Models.ZoomMode.FitPage or HyperPDF.Models.ZoomMode.FitWidth)
        {
            ApplyFitZoom();
        }
    }

    /// <summary>Applies the fit zoom strategy to the current viewport.</summary>
    public void ApplyFitZoom()
    {
        if (_document is null)
        {
            return;
        }

        double sidebarWidth = Sidebar.Visibility == Visibility.Visible ? Sidebar.ActualWidth : 0;
        double width = Math.Max(1, ActualWidth - sidebarWidth - 32);
        double height = Math.Max(1, ActualHeight - 32);

        _document.ApplyZoomToViewport(width, height);
    }

    private void OnDocumentPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(DocumentViewModel.CurrentPageIndex))
        {
            SyncThumbnailSelection();
        }
    }

    private void OnScrollToPageRequested(object? sender, int pageIndex) => ScrollToPage(pageIndex);

    private void OnThumbnailSelected(object sender, SelectionChangedEventArgs e)
    {
        if (_isSyncingSelection || _document is null)
        {
            return;
        }

        if (ThumbnailList.SelectedIndex >= 0)
        {
            _document.GoToPageIndex(ThumbnailList.SelectedIndex);
        }
    }

    private void SyncThumbnailSelection()
    {
        if (_document is null)
        {
            return;
        }

        _isSyncingSelection = true;
        try
        {
            ThumbnailList.SelectedIndex = _document.CurrentPageIndex;
        }
        finally
        {
            _isSyncingSelection = false;
        }
    }

    private void OnScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        if (e.VerticalChange == 0 || _document is null)
        {
            return;
        }

        long tick = Environment.TickCount64;
        if (tick - _lastScrollTick < 80)
        {
            return;
        }

        _lastScrollTick = tick;

        int page = PageAtOffset(PageScrollViewer.VerticalOffset);
        if (page >= 0 && page != _document.CurrentPageIndex)
        {
            _document.SetCurrentPage(page);
        }
    }

    private int PageAtOffset(double offset)
    {
        if (_document is null || _document.Pages.Count == 0)
        {
            return -1;
        }

        double current = 0;
        for (int index = 0; index < _document.Pages.Count; index++)
        {
            double height = _document.Pages[index].ContainerHeight + PageSpacing;
            if (offset < current + (height / 2))
            {
                return index;
            }

            current += height;
        }

        return _document.Pages.Count - 1;
    }

    private double OffsetOfPage(int index)
    {
        if (_document is null)
        {
            return 0;
        }

        double offset = PageSpacing;
        for (int i = 0; i < index && i < _document.Pages.Count; i++)
        {
            offset += _document.Pages[i].ContainerHeight + PageSpacing;
        }

        return offset;
    }

    private async void OnFindKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            e.Handled = true;
            await RunSearchAsync();
        }
        else if (e.Key == Key.Escape)
        {
            e.Handled = true;
            HideFindBar();
        }
    }

    private async Task RunSearchAsync()
    {
        if (_document is null)
        {
            return;
        }

        string term = FindTextBox.Text;
        if (string.IsNullOrWhiteSpace(term))
        {
            _document.ClearSearch();
            return;
        }

        await _document.FindAsync(term).ConfigureAwait(true);
    }
}
