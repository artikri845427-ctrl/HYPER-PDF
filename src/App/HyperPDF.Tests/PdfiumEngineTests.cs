// Author: Anwarul Haque (@HYPERMX7) | Contact: 7739399007
using HyperPDF.Models;
using HyperPDF.Services;
using HyperPDF.Services.Pdfium;
using Xunit;

namespace HyperPDF.Tests;

/// <summary>
/// Integration tests that exercise the native PDFium engine end to end: opening a document,
/// rasterising a page, extracting the text layer and searching inside the document.
/// </summary>
public sealed class PdfiumEngineTests : IDisposable
{
    private readonly string _pdfPath = TestPdfBuilder.CreateFile(3);

    public void Dispose()
    {
        try
        {
            if (File.Exists(_pdfPath))
            {
                File.Delete(_pdfPath);
            }
        }
        catch (IOException)
        {
            // Ignore clean up failures in the temporary folder.
        }
    }

    [Fact]
    public void DocumentOpensAndReportsThePageCount()
    {
        using PdfDocument document = PdfDocument.Open(_pdfPath);

        Assert.True(document.IsOpen);
        Assert.Equal(3, document.PageCount);
        Assert.EndsWith(".pdf", document.FileName, StringComparison.OrdinalIgnoreCase);
        Assert.False(document.IsEncrypted);
        Assert.True(document.PdfVersion >= 10, $"Unexpected PDF version {document.PdfVersion}.");
    }

    [Fact]
    public void PageSizeIsReportedInPoints()
    {
        using PdfDocument document = PdfDocument.Open(_pdfPath);

        var size = document.GetPageSize(0);

        Assert.Equal(595f, size.Width, 2);
        Assert.Equal(842f, size.Height, 2);
    }

    [Fact]
    public void OutOfRangePageIndexThrows()
    {
        using PdfDocument document = PdfDocument.Open(_pdfPath);

        Assert.Throws<ArgumentOutOfRangeException>(() => document.GetPageSize(99));
    }

    [Fact]
    public void PageIsRenderedIntoABitmap()
    {
        using PdfDocument document = PdfDocument.Open(_pdfPath);

        RenderedPage page = PdfiumRenderer.Render(document, 0, 400, 566);

        Assert.Equal(400, page.Width);
        Assert.Equal(566, page.Height);
        Assert.Equal(400 * 566 * 4, page.Pixels.Length);
        Assert.True(page.PageWidthPoints > 0);
        Assert.True(page.PageHeightPoints > 0);
    }

    [Fact]
    public void RenderedPageContainsNonWhitePixels()
    {
        using PdfDocument document = PdfDocument.Open(_pdfPath);

        RenderedPage page = PdfiumRenderer.Render(document, 0, 400, 566);

        int nonWhite = 0;
        for (int i = 0; i < page.Pixels.Length; i += 4)
        {
            byte b = page.Pixels[i];
            byte g = page.Pixels[i + 1];
            byte r = page.Pixels[i + 2];
            if (r < 240 || g < 240 || b < 240)
            {
                nonWhite++;
            }
        }

        Assert.True(nonWhite > 200, $"The rendered page is blank (non white pixels: {nonWhite}).");
    }

    [Fact]
    public void RenderedPageCanBeConvertedIntoAWpfBitmapSource()
    {
        using PdfDocument document = PdfDocument.Open(_pdfPath);

        RenderedPage page = PdfiumRenderer.Render(document, 0, 200, 283);

        var source = page.ToBitmapSource();

        Assert.Equal(200, source.PixelWidth);
        Assert.Equal(283, source.PixelHeight);
        Assert.True(source.IsFrozen);
    }

    [Fact]
    public void TextLayerIsExtracted()
    {
        using PdfDocument document = PdfDocument.Open(_pdfPath);

        string text = PdfiumTextService.ExtractPageText(document, 0);

        Assert.Contains("HyperPDF", text, StringComparison.Ordinal);
        Assert.Contains("sample page 1", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SearchFindsEveryOccurrence()
    {
        using PdfDocument document = PdfDocument.Open(_pdfPath);

        IReadOnlyList<TextSearchMatch> matches = PdfiumTextService.Search(document, "HyperPDF");

        Assert.True(matches.Count >= 3, $"Expected at least 3 hits, found {matches.Count}.");
        Assert.Contains(matches, m => m.PageIndex == 0);
        Assert.Contains(matches, m => m.PageIndex == 2);
    }

    [Fact]
    public void SearchIsCaseInsensitiveByDefaultAndCaseSensitiveOnDemand()
    {
        using PdfDocument document = PdfDocument.Open(_pdfPath);

        IReadOnlyList<TextSearchMatch> insensitive = PdfiumTextService.Search(document, "hyperpdf");
        IReadOnlyList<TextSearchMatch> sensitive = PdfiumTextService.Search(document, "hyperpdf", matchCase: true);

        Assert.NotEmpty(insensitive);
        Assert.Empty(sensitive);
    }

    [Fact]
    public void SearchHitsCarryHighlightRectangles()
    {
        using PdfDocument document = PdfDocument.Open(_pdfPath);

        IReadOnlyList<TextSearchMatch> matches = PdfiumTextService.Search(document, "HyperPDF");

        Assert.NotEmpty(matches);
        Assert.All(matches, m => Assert.NotEmpty(m.Rectangles));
        Assert.All(matches, m => Assert.True(m.Rectangles[0].Right > m.Rectangles[0].Left));
    }

    [Fact]
    public void SearchForAnUnknownTermReturnsNoHits()
    {
        using PdfDocument document = PdfDocument.Open(_pdfPath);

        IReadOnlyList<TextSearchMatch> matches = PdfiumTextService.Search(document, "zzz-not-present-zzz");

        Assert.Empty(matches);
    }

    [Fact]
    public void FileIdentifierIsAvailable()
    {
        using PdfDocument document = PdfDocument.Open(_pdfPath);

        string identifier = document.GetFileIdentifier();

        Assert.False(string.IsNullOrWhiteSpace(identifier));
    }

    [Fact]
    public void OpeningAMissingFileThrowsAPdfOpenException()
    {
        var exception = Assert.Throws<PdfOpenException>(() => PdfDocument.Open(Path.Combine(Path.GetTempPath(), "does-not-exist-" + Guid.NewGuid().ToString("N") + ".pdf")));

        Assert.Equal(PdfOpenStatus.FileNotFound, exception.Status);
    }

    [Fact]
    public void OpeningADamagedFileThrowsAPdfOpenException()
    {
        string path = Path.Combine(Path.GetTempPath(), $"hyperpdf-broken-{Guid.NewGuid():N}.pdf");
        File.WriteAllText(path, "this is definitely not a pdf file");

        try
        {
            var exception = Assert.Throws<PdfOpenException>(() => PdfDocument.Open(path));
            Assert.NotEqual(PdfOpenStatus.Unknown, exception.Status);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
