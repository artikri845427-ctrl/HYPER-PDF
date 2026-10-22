// Author: Anwarul Haque (@HYPERMX7) | Contact: 7739399007
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using HyperPDF.ViewModels;

namespace HyperPDF.Controls;

/// <summary>
/// Renders a single PDF page. The control asks the document view model for a bitmap that
/// matches the current zoom level and the display resolution of the monitor.
/// </summary>
public partial class PdfPageView : UserControl
{
    private CancellationTokenSource? _renderCancellation;
    private int _renderToken;
    private bool _isSizeKnown;
    private Window? _parentWindow;

    /// <summary>Creates the control.</summary>
    public PdfPageView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        SizeChanged += OnSizeChanged;
    }

    /// <summary>The page that is rendered by this control.</summary>
    public static readonly DependencyProperty PageProperty = DependencyProperty.Register(
        nameof(Page),
        typeof(PageViewModel),
        typeof(PdfPageView),
        new PropertyMetadata(null, OnRenderDependencyChanged));

    /// <summary>The document that owns the page.</summary>
    public static readonly DependencyProperty DocumentProperty = DependencyProperty.Register(
        nameof(Document),
        typeof(DocumentViewModel),
        typeof(PdfPageView),
        new PropertyMetadata(null, OnRenderDependencyChanged));

    /// <summary>The page that is rendered by this control.</summary>
    public PageViewModel? Page
    {
        get => (PageViewModel?)GetValue(PageProperty);
        set => SetValue(PageProperty, value);
    }

    /// <summary>The document that owns the page.</summary>
    public DocumentViewModel? Document
    {
        get => (DocumentViewModel?)GetValue(DocumentProperty);
        set => SetValue(DocumentProperty, value);
    }

    private static void OnRenderDependencyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is PdfPageView control)
        {
            control.OnRenderDependencyChanged(e.Property);
        }
    }

    private void OnRenderDependencyChanged(DependencyProperty property)
    {
        if (property == DocumentProperty)
        {
            if (Document is not null)
            {
                Document.PropertyChanged += OnDocumentPropertyChanged;
            }
        }

        if (property == PageProperty)
        {
            if (Page is not null)
            {
                Page.PropertyChanged += OnPagePropertyChanged;
            }
        }

        ApplySize();
        ScheduleRender();
    }

    private void OnPagePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(PageViewModel.DisplayWidth)
            or nameof(PageViewModel.DisplayHeight)
            or nameof(PageViewModel.ContainerWidth)
            or nameof(PageViewModel.ContainerHeight))
        {
            ApplySize();
            ScheduleRender();
        }
    }

    private void OnDocumentPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(DocumentViewModel.RenderVersion)
            or nameof(DocumentViewModel.Rotation)
            or nameof(DocumentViewModel.ZoomPercent))
        {
            ScheduleRender();
        }
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _parentWindow = Window.GetWindow(this);
        if (_parentWindow is not null)
        {
            _parentWindow.DpiChanged += OnWindowDpiChanged;
        }

        ScheduleRender();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (_parentWindow is not null)
        {
            _parentWindow.DpiChanged -= OnWindowDpiChanged;
            _parentWindow = null;
        }

        _renderCancellation?.Cancel();
        _renderCancellation?.Dispose();
        _renderCancellation = null;
    }

    private void OnWindowDpiChanged(object sender, DpiChangedEventArgs e) => ScheduleRender();

    private void OnSizeChanged(object sender, SizeChangedEventArgs e) => ScheduleRender();

    private void ScheduleRender()
    {
        if (!IsLoaded)
        {
            return;
        }

        _renderCancellation?.Cancel();
        _renderCancellation?.Dispose();

        var cancellation = new CancellationTokenSource();
        _renderCancellation = cancellation;

        int token = ++_renderToken;
        _ = RenderAsync(token, cancellation.Token);
    }

    private async Task RenderAsync(int token, CancellationToken cancellationToken)
    {
        PageViewModel? page = Page;
        DocumentViewModel? document = Document;

        if (page is null || document is null)
        {
            return;
        }

        double dpiScale = 1.0;
        try
        {
            dpiScale = VisualTreeHelper.GetDpi(this).DpiScaleX;
        }
        catch (InvalidOperationException)
        {
            // The control is not connected to a presentation source yet.
        }

        int pixelWidth = (int)Math.Round(page.DisplayWidth * dpiScale);
        int pixelHeight = (int)Math.Round(page.DisplayHeight * dpiScale);

        if (pixelWidth < 1 || pixelHeight < 1)
        {
            return;
        }

        if (!_isSizeKnown)
        {
            _isSizeKnown = true;
        }

        BitmapSource? bitmap = await document.RenderPageAsync(page.PageIndex, pixelWidth, pixelHeight, cancellationToken)
            .ConfigureAwait(true);

        if (bitmap is null || cancellationToken.IsCancellationRequested || token != _renderToken)
        {
            return;
        }

        PageImage.Source = bitmap;
        PageImage.Visibility = Visibility.Visible;
        Placeholder.Visibility = Visibility.Collapsed;

        ApplyRotation();
        ApplySize();
    }

    private void ApplySize()
    {
        PageViewModel? page = Page;
        if (page is null)
        {
            return;
        }

        PageBorder.Width = page.ContainerWidth;
        PageBorder.Height = page.ContainerHeight;
        PageImage.Width = page.DisplayWidth;
        PageImage.Height = page.DisplayHeight;
    }

    private void ApplyRotation()
    {
        PageViewModel? page = Page;
        DocumentViewModel? document = Document;

        int rotation = document?.Rotation ?? 0;
        if (page is null)
        {
            return;
        }

        PageImage.RenderTransformOrigin = new System.Windows.Point(0.5, 0.5);
        PageImage.RenderTransform = rotation switch
        {
            90 => new RotateTransform(90),
            180 => new RotateTransform(180),
            270 => new RotateTransform(270),
            _ => Transform.Identity
        };
    }
}
