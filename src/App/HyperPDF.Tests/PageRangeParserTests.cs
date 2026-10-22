// Author: Anwarul Haque (@HYPERMX7) | Contact: 7739399007
using HyperPDF.Models;
using Xunit;

namespace HyperPDF.Tests;

/// <summary>Tests the printer style page range parser.</summary>
public sealed class PageRangeParserTests
{
    [Fact]
    public void EmptyExpressionSelectsEveryPage()
    {
        bool ok = PageRangeParser.TryParse(null, 5, out IReadOnlyList<int> pages, out string? error);

        Assert.True(ok);
        Assert.Null(error);
        Assert.Equal(new[] { 1, 2, 3, 4, 5 }, pages);
    }

    [Fact]
    public void SinglePagesAreResolved()
    {
        bool ok = PageRangeParser.TryParse("2", 5, out IReadOnlyList<int> pages, out string? error);

        Assert.True(ok);
        Assert.Null(error);
        Assert.Equal(new[] { 2 }, pages);
    }

    [Fact]
    public void RangesAreExpanded()
    {
        bool ok = PageRangeParser.TryParse("1-3, 5", 8, out IReadOnlyList<int> pages, out _);

        Assert.True(ok);
        Assert.Equal(new[] { 1, 2, 3, 5 }, pages);
    }

    [Fact]
    public void OpenEndedRangeIsClampedToTheDocument()
    {
        bool ok = PageRangeParser.TryParse("4-", 5, out IReadOnlyList<int> pages, out _);

        Assert.True(ok);
        Assert.Equal(new[] { 4, 5 }, pages);
    }

    [Fact]
    public void DuplicatesAreRemovedAndOrderIsAscending()
    {
        bool ok = PageRangeParser.TryParse("3, 1-2, 3", 5, out IReadOnlyList<int> pages, out _);

        Assert.True(ok);
        Assert.Equal(new[] { 1, 2, 3 }, pages);
    }

    [Fact]
    public void PageOutsideTheDocumentIsRejected()
    {
        bool ok = PageRangeParser.TryParse("9", 3, out _, out string? error);

        Assert.False(ok);
        Assert.NotNull(error);
        Assert.Contains("outside the document", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ReversedRangeIsRejected()
    {
        bool ok = PageRangeParser.TryParse("4-2", 8, out _, out string? error);

        Assert.False(ok);
        Assert.NotNull(error);
    }

    [Fact]
    public void NonNumericInputIsRejected()
    {
        bool ok = PageRangeParser.TryParse("abc", 8, out _, out string? error);

        Assert.False(ok);
        Assert.NotNull(error);
    }

    [Fact]
    public void EmptyDocumentIsRejected()
    {
        bool ok = PageRangeParser.TryParse("1", 0, out _, out string? error);

        Assert.False(ok);
        Assert.NotNull(error);
    }

    [Theory]
    [InlineData(new[] { 1 }, "1")]
    [InlineData(new[] { 1, 2, 3 }, "1-3")]
    [InlineData(new[] { 1, 2, 3, 5 }, "1-3, 5")]
    [InlineData(new[] { 2, 4, 6 }, "2, 4, 6")]
    public void FormatProducesCompactRanges(int[] pages, string expected)
    {
        Assert.Equal(expected, PageRangeParser.Format(pages));
    }

    [Fact]
    public void FormatAndParseAreRoundTripCompatible()
    {
        int[] pages = { 1, 2, 3, 7, 9, 10 };

        string formatted = PageRangeParser.Format(pages);

        Assert.True(PageRangeParser.TryParse(formatted, 12, out IReadOnlyList<int> parsed, out _));
        Assert.Equal(pages, parsed);
    }

    [Fact]
    public void FormatOfAnEmptyListIsEmpty()
    {
        Assert.Equal(string.Empty, PageRangeParser.Format(Array.Empty<int>()));
    }
}
