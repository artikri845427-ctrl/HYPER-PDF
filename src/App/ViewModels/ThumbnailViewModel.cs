// Author: Anwarul Haque (@HYPERMX7) | Contact: 7739399007
using System.Windows.Media.Imaging;

namespace HyperPDF.ViewModels;

/// <summary>View model of a single entry in the thumbnail sidebar.</summary>
public sealed class ThumbnailViewModel : ViewModelBase
{
    private BitmapSource? _image;
    private bool _isLoading;

    /// <summary>Creates a new thumbnail view model.</summary>
    public ThumbnailViewModel(int pageIndex, string pageLabel)
    {
        PageIndex = pageIndex;
        PageLabel = pageLabel;
    }

    /// <summary>Zero based index of the page.</summary>
    public int PageIndex { get; }

    /// <summary>One based page number.</summary>
    public int PageNumber => PageIndex + 1;

    /// <summary>Page label of the document, when it defines one.</summary>
    public string PageLabel { get; }

    /// <summary>The rendered preview.</summary>
    public BitmapSource? Image
    {
        get => _image;
        set => SetProperty(ref _image, value);
    }

    /// <summary>True while the preview is being generated.</summary>
    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }
}
