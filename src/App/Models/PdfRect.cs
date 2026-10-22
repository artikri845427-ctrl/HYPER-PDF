// Author: Anwarul Haque (@HYPERMX7) | Contact: 7739399007
namespace HyperPDF.Models;

/// <summary>
/// A rectangle expressed in PDF page space (points, origin in the bottom left corner).
/// </summary>
public readonly record struct PdfRect(double Left, double Bottom, double Right, double Top)
{
    /// <summary>True when the rectangle has a usable (positive) area.</summary>
    public bool IsValid => Right > Left && Top > Bottom;

    /// <summary>Width of the rectangle in points.</summary>
    public double Width => Right - Left;

    /// <summary>Height of the rectangle in points.</summary>
    public double Height => Top - Bottom;
}
