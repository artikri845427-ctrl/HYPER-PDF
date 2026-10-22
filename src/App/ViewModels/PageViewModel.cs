// Author: Anwarul Haque (@HYPERMX7) | Contact: 7739399007
using HyperPDF.Models;

namespace HyperPDF.ViewModels;

/// <summary>View model of a single page inside the document workspace.</summary>
public sealed class PageViewModel : ViewModelBase
{
    private double _displayWidth;
    private double _displayHeight;
    private double _containerWidth;
    private double _containerHeight;

    /// <summary>Creates a new page view model.</summary>
    public PageViewModel(int pageIndex, float widthPoints, float heightPoints)
    {
        PageIndex = pageIndex;
        WidthPoints = widthPoints;
        HeightPoints = heightPoints;
    }

    /// <summary>Zero based index of the page inside the document.</summary>
    public int PageIndex { get; }

    /// <summary>One based page number.</summary>
    public int PageNumber => PageIndex + 1;

    /// <summary>Width of the page in PDF points.</summary>
    public float WidthPoints { get; }

    /// <summary>Height of the page in PDF points.</summary>
    public float HeightPoints { get; }

    /// <summary>Page label of the document (for example "iv"), when it defines one.</summary>
    public string PageLabel { get; set; } = string.Empty;

    /// <summary>Width of the rendered page in device independent pixels, before rotation.</summary>
    public double DisplayWidth
    {
        get => _displayWidth;
        private set => SetProperty(ref _displayWidth, value);
    }

    /// <summary>Height of the rendered page in device independent pixels, before rotation.</summary>
    public double DisplayHeight
    {
        get => _displayHeight;
        private set => SetProperty(ref _displayHeight, value);
    }

    /// <summary>Width of the space occupied by the page, after the rotation is applied.</summary>
    public double ContainerWidth
    {
        get => _containerWidth;
        private set => SetProperty(ref _containerWidth, value);
    }

    /// <summary>Height of the space occupied by the page, after the rotation is applied.</summary>
    public double ContainerHeight
    {
        get => _containerHeight;
        private set => SetProperty(ref _containerHeight, value);
    }

    /// <summary>Updates the display metrics after a zoom or rotation change.</summary>
    public void UpdateMetrics(double scale, bool swapDimensions)
    {
        double width = WidthPoints * scale;
        double height = HeightPoints * scale;

        DisplayWidth = Math.Max(1, Math.Round(width));
        DisplayHeight = Math.Max(1, Math.Round(height));

        if (swapDimensions)
        {
            ContainerWidth = DisplayHeight;
            ContainerHeight = DisplayWidth;
        }
        else
        {
            ContainerWidth = DisplayWidth;
            ContainerHeight = DisplayHeight;
        }
    }
}
