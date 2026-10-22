// Author: Anwarul Haque (@HYPERMX7) | Contact: 7739399007
namespace HyperPDF.Models;

/// <summary>A single search hit inside a document.</summary>
public sealed class TextSearchMatch
{
    /// <summary>Creates a new match.</summary>
    public TextSearchMatch(int pageIndex, int startIndex, int length, IReadOnlyList<PdfRect> rectangles)
    {
        PageIndex = pageIndex;
        StartIndex = startIndex;
        Length = length;
        Rectangles = rectangles;
    }

    /// <summary>Zero based index of the page that contains the hit.</summary>
    public int PageIndex { get; }

    /// <summary>Zero based index of the first character of the hit inside the page text.</summary>
    public int StartIndex { get; }

    /// <summary>Number of characters in the hit.</summary>
    public int Length { get; }

    /// <summary>Character boxes covered by the hit, in PDF page space.</summary>
    public IReadOnlyList<PdfRect> Rectangles { get; }

    /// <summary>One based page number of the hit.</summary>
    public int PageNumber => PageIndex + 1;
}
