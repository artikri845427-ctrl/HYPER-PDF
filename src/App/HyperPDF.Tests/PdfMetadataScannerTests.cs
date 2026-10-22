// Author: Anwarul Haque (@HYPERMX7) | Contact: 7739399007
using HyperPDF.Services;
using Xunit;

namespace HyperPDF.Tests;

/// <summary>Tests the document information scanner that reads the PDF info dictionary.</summary>
public sealed class PdfMetadataScannerTests
{
    [Fact]
    public void TitleAndAuthorAreReadFromTheInfoDictionary()
    {
        string path = TestPdfBuilder.CreateFile(1, "Quarterly Report 2026");

        try
        {
            var info = PdfMetadataScanner.Scan(path);

            Assert.Equal("Quarterly Report 2026", info.Title);
            Assert.Equal("Anwarul Haque (@HYPERMX7)", info.Author);
            Assert.Equal("HyperPDF", info.Creator);
            Assert.Equal("HyperPDF 1.0.0", info.Producer);
            Assert.Equal(path, info.FilePath);
            Assert.True(info.FileSize > 0);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void AMissingFileDoesNotThrow()
    {
        string path = Path.Combine(Path.GetTempPath(), $"missing-{Guid.NewGuid():N}.pdf");

        var info = PdfMetadataScanner.Scan(path);

        Assert.Equal(path, info.FilePath);
        Assert.Equal(string.Empty, info.Title);
    }

    [Fact]
    public void AnEmptyFileDoesNotThrow()
    {
        string path = Path.Combine(Path.GetTempPath(), $"empty-{Guid.NewGuid():N}.pdf");
        File.WriteAllBytes(path, Array.Empty<byte>());

        try
        {
            var info = PdfMetadataScanner.Scan(path);

            Assert.Equal(string.Empty, info.Title);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
