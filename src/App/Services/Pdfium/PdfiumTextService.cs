// Author: Anwarul Haque (@HYPERMX7) | Contact: 7739399007
using System.Text;
using HyperPDF.Models;

namespace HyperPDF.Services.Pdfium;

/// <summary>Extracts and searches the text layer of a PDF document using PDFium.</summary>
public static class PdfiumTextService
{
    /// <summary>Default upper bound for the number of returned hits.</summary>
    public const int DefaultMaxMatches = 2000;

    /// <summary>
    /// Extracts the plain text of a page. Returns an empty string for pages without a text layer
    /// (for example scanned images).
    /// </summary>
    public static string ExtractPageText(PdfDocument document, int pageIndex)
    {
        ArgumentNullException.ThrowIfNull(document);

        lock (document.SyncRoot)
        {
            IntPtr page = PdfiumNative.FPDF_LoadPage(document.Handle, pageIndex);
            if (page == IntPtr.Zero)
            {
                return string.Empty;
            }

            IntPtr textPage = IntPtr.Zero;
            try
            {
                textPage = PdfiumNative.FPDFText_LoadPage(page);
                if (textPage == IntPtr.Zero)
                {
                    return string.Empty;
                }

                int count = PdfiumNative.FPDFText_CountChars(textPage);
                if (count <= 0)
                {
                    return string.Empty;
                }

                var buffer = new ushort[count];
                int copied = PdfiumNative.FPDFText_GetText(textPage, 0, count, buffer);
                if (copied <= 0)
                {
                    return string.Empty;
                }

                var builder = new StringBuilder(copied);
                for (int i = 0; i < copied && i < buffer.Length; i++)
                {
                    builder.Append((char)buffer[i]);
                }

                return builder.ToString();
            }
            finally
            {
                if (textPage != IntPtr.Zero)
                {
                    PdfiumNative.FPDFText_ClosePage(textPage);
                }

                PdfiumNative.FPDF_ClosePage(page);
            }
        }
    }

    /// <summary>
    /// Searches the whole document for a term and returns the position and the highlight
    /// rectangles of every hit.
    /// </summary>
    /// <param name="document">The document to search.</param>
    /// <param name="term">The search term.</param>
    /// <param name="matchCase">True for a case sensitive search.</param>
    /// <param name="maxMatches">Upper bound of returned hits.</param>
    public static IReadOnlyList<TextSearchMatch> Search(
        PdfDocument document,
        string term,
        bool matchCase = false,
        int maxMatches = DefaultMaxMatches)
    {
        ArgumentNullException.ThrowIfNull(document);

        var results = new List<TextSearchMatch>();
        if (string.IsNullOrEmpty(term))
        {
            return results;
        }

        StringComparison comparison = matchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

        lock (document.SyncRoot)
        {
            for (int pageIndex = 0; pageIndex < document.PageCount; pageIndex++)
            {
                if (results.Count >= maxMatches)
                {
                    break;
                }

                IntPtr page = PdfiumNative.FPDF_LoadPage(document.Handle, pageIndex);
                if (page == IntPtr.Zero)
                {
                    continue;
                }

                IntPtr textPage = IntPtr.Zero;
                try
                {
                    textPage = PdfiumNative.FPDFText_LoadPage(page);
                    if (textPage == IntPtr.Zero)
                    {
                        continue;
                    }

                    int count = PdfiumNative.FPDFText_CountChars(textPage);
                    if (count <= 0)
                    {
                        continue;
                    }

                    var buffer = new ushort[count];
                    int copied = PdfiumNative.FPDFText_GetText(textPage, 0, count, buffer);
                    if (copied <= 0)
                    {
                        continue;
                    }

                    var builder = new StringBuilder(copied);
                    for (int i = 0; i < copied && i < buffer.Length; i++)
                    {
                        builder.Append((char)buffer[i]);
                    }

                    string text = builder.ToString();
                    int index = text.IndexOf(term, comparison);
                    while (index >= 0 && results.Count < maxMatches)
                    {
                        IReadOnlyList<PdfRect> rectangles = GetCharacterBoxes(textPage, index, term.Length);
                        results.Add(new TextSearchMatch(pageIndex, index, term.Length, rectangles));

                        int next = index + Math.Max(1, term.Length);
                        index = next >= text.Length ? -1 : text.IndexOf(term, next, comparison);
                    }
                }
                finally
                {
                    if (textPage != IntPtr.Zero)
                    {
                        PdfiumNative.FPDFText_ClosePage(textPage);
                    }

                    PdfiumNative.FPDF_ClosePage(page);
                }
            }
        }

        return results;
    }

    private static IReadOnlyList<PdfRect> GetCharacterBoxes(IntPtr textPage, int startIndex, int length)
    {
        var rectangles = new List<PdfRect>();
        if (length <= 0)
        {
            return rectangles;
        }

        double left = double.MaxValue;
        double right = double.MinValue;
        double bottom = double.MaxValue;
        double top = double.MinValue;
        bool any = false;

        int end = Math.Min(startIndex + length, int.MaxValue);
        for (int index = startIndex; index < end; index++)
        {
            if (!PdfiumNative.FPDFText_GetCharBox(textPage, index, out double charLeft, out double charRight, out double charBottom, out double charTop))
            {
                continue;
            }

            if (double.IsNaN(charLeft) || double.IsNaN(charRight) || double.IsNaN(charBottom) || double.IsNaN(charTop))
            {
                continue;
            }

            if (charRight <= charLeft && charTop <= charBottom)
            {
                continue;
            }

            any = true;
            left = Math.Min(left, charLeft);
            right = Math.Max(right, charRight);
            bottom = Math.Min(bottom, charBottom);
            top = Math.Max(top, charTop);
        }

        if (any)
        {
            rectangles.Add(new PdfRect(left, bottom, right, top));
        }

        return rectangles;
    }
}
