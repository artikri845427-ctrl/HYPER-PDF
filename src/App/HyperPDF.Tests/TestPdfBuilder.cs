// Author: Anwarul Haque (@HYPERMX7) | Contact: 7739399007
using System.Text;

namespace HyperPDF.Tests;

/// <summary>
/// Builds small but valid PDF files on the fly so that the tests do not depend on any
/// binary fixture inside the repository.
/// </summary>
internal static class TestPdfBuilder
{
    /// <summary>Creates a PDF with the given number of pages and returns its bytes.</summary>
    public static byte[] Create(int pageCount = 3, string title = "HyperPDF Sample Document")
    {
        if (pageCount < 1)
        {
            pageCount = 1;
        }

        var objects = new List<string>();

        var pageNumbers = new List<int>();
        var contentNumbers = new List<int>();
        int next = 5;
        for (int i = 0; i < pageCount; i++)
        {
            pageNumbers.Add(next);
            contentNumbers.Add(next + 1);
            next += 2;
        }

        objects.Add("<< /Type /Catalog /Pages 2 0 R >>");
        string kids = string.Join(" ", pageNumbers.Select(n => $"{n} 0 R"));
        objects.Add($"<< /Type /Pages /Kids [{kids}] /Count {pageCount} >>");
        objects.Add("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>");
        objects.Add($"<< /Title ({title}) /Author (Anwarul Haque (@HYPERMX7)) /Creator (HyperPDF) /Producer (HyperPDF 1.0.0) >>");

        for (int i = 0; i < pageCount; i++)
        {
            objects.Add(
                $"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 3 0 R >> >> /Contents {contentNumbers[i]} 0 R >>");

            string text = $"HyperPDF sample page {i + 1}";
            string stream =
                $"BT /F1 28 Tf 60 740 Td ({text}) Tj ET\n" +
                "BT /F1 14 Tf 60 700 Td (Developed by Anwarul Haque (@HYPERMX7) | Contact: 7739399007) Tj ET";
            objects.Add($"<< /Length {stream.Length} >>\nstream\n{stream}\nendstream");
        }

        var bytes = new List<byte>();
        var offsets = new List<int>();

        void Add(string text) => bytes.AddRange(Encoding.Latin1.GetBytes(text));

        Add("%PDF-1.7\n");

        for (int i = 0; i < objects.Count; i++)
        {
            offsets.Add(bytes.Count);
            Add($"{i + 1} 0 obj\n{objects[i]}\nendobj\n");
        }

        int xrefOffset = bytes.Count;
        int count = objects.Count + 1;

        Add($"xref\n0 {count}\n");
        Add("0000000000 65535 f \n");
        foreach (int offset in offsets)
        {
            Add($"{offset:D10} 00000 n \n");
        }

        // A conforming PDF carries a file identifier, PDFium returns it through FPDF_GetFileIdentifier.
        const string fileId = "0123456789ABCDEF0123456789ABCDEF";
        Add($"trailer\n<< /Size {count} /Root 1 0 R /Info 4 0 R /ID [<{fileId}> <{fileId}>] >>\nstartxref\n{xrefOffset}\n%%EOF\n");

        return bytes.ToArray();
    }

    /// <summary>Writes a sample PDF into a temporary file and returns its path.</summary>
    public static string CreateFile(int pageCount = 3, string? title = null)
    {
        string path = Path.Combine(Path.GetTempPath(), $"hyperpdf-test-{Guid.NewGuid():N}.pdf");
        File.WriteAllBytes(path, Create(pageCount, title ?? "HyperPDF Sample Document"));
        return path;
    }
}
