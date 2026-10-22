// Author: Anwarul Haque (@HYPERMX7) | Contact: 7739399007
namespace HyperPDF.Models;

/// <summary>Colour theme of the application.</summary>
public enum AppTheme
{
    /// <summary>Light theme.</summary>
    Light = 0,

    /// <summary>Dark theme.</summary>
    Dark = 1
}

/// <summary>How pages are laid out in the document workspace.</summary>
public enum ViewMode
{
    /// <summary>One page at a time.</summary>
    SinglePage = 0,

    /// <summary>All pages stacked vertically with virtualised scrolling.</summary>
    Continuous = 1
}

/// <summary>Zoom strategy applied when a document is opened.</summary>
public enum ZoomMode
{
    /// <summary>Use a fixed zoom percentage.</summary>
    Custom = 0,

    /// <summary>Fit the whole page into the viewport.</summary>
    FitPage = 1,

    /// <summary>Fit the page width into the viewport.</summary>
    FitWidth = 2
}
