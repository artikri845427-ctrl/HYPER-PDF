// Author: Anwarul Haque (@HYPERMX7) | Contact: 7739399007
using System.Text.Json.Serialization;

namespace HyperPDF.Models;

/// <summary>Remembers where the user stopped reading a document.</summary>
public sealed class DocumentSessionState
{
    /// <summary>Zero based index of the page that was visible last.</summary>
    public int LastPageIndex { get; set; }

    /// <summary>Zoom percentage that was used last.</summary>
    public double ZoomPercent { get; set; } = 100;

    /// <summary>Page layout that was used last.</summary>
    public ViewMode ViewMode { get; set; } = ViewMode.Continuous;
}

/// <summary>Remembers the position and the size of the main window.</summary>
public sealed class WindowPlacementState
{
    /// <summary>Distance to the left edge of the virtual desktop, or null when unknown.</summary>
    public double? Left { get; set; }

    /// <summary>Distance to the top edge of the virtual desktop, or null when unknown.</summary>
    public double? Top { get; set; }

    /// <summary>Width of the window in device independent pixels.</summary>
    public double Width { get; set; } = 1240;

    /// <summary>Height of the window in device independent pixels.</summary>
    public double Height { get; set; } = 820;

    /// <summary>True when the window was maximised.</summary>
    public bool IsMaximized { get; set; }

    /// <summary>True when the window was in full screen mode.</summary>
    public bool IsFullScreen { get; set; }

    /// <summary>True when the window has a usable position.</summary>
    [JsonIgnore]
    public bool HasValidPosition => Left.HasValue && Top.HasValue;
}
