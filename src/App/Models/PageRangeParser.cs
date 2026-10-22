// Author: Anwarul Haque (@HYPERMX7) | Contact: 7739399007
using System.Globalization;
using System.Text;

namespace HyperPDF.Models;

/// <summary>
/// Parses printer style page range expressions such as <c>"1-3, 7, 10-"</c>.
/// </summary>
public static class PageRangeParser
{
    /// <summary>
    /// Parses a page range expression.
    /// </summary>
    /// <param name="expression">The expression to parse. <c>null</c> or empty selects every page.</param>
    /// <param name="pageCount">Total number of pages in the document.</param>
    /// <param name="pages">The resolved, ascending list of one based page numbers.</param>
    /// <param name="error">A human readable error message when the expression is invalid.</param>
    /// <returns><c>true</c> when the expression could be parsed.</returns>
    public static bool TryParse(string? expression, int pageCount, out IReadOnlyList<int> pages, out string? error)
    {
        pages = Array.Empty<int>();

        if (pageCount <= 0)
        {
            error = "The document does not contain any pages.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(expression))
        {
            pages = Enumerable.Range(1, pageCount).ToArray();
            error = null;
            return true;
        }

        var selected = new SortedSet<int>();
        var errors = new List<string>();

        foreach (string rawToken in expression.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            string token = rawToken;
            if (token.Length == 0)
            {
                continue;
            }

            int separator = token.IndexOf('-', StringComparison.Ordinal);
            if (separator < 0)
            {
                if (!TryParseNumber(token, out int single))
                {
                    errors.Add($"'{token}' is not a valid page number.");
                    continue;
                }

                if (single < 1 || single > pageCount)
                {
                    errors.Add($"Page {single} is outside the document (1-{pageCount}).");
                    continue;
                }

                selected.Add(single);
                continue;
            }

            string startText = token[..separator].Trim();
            string endText = token[(separator + 1)..].Trim();

            int start = 1;
            if (startText.Length > 0 && !TryParseNumber(startText, out start))
            {
                errors.Add($"'{startText}' is not a valid page number.");
                continue;
            }

            int end = pageCount;
            if (endText.Length > 0 && !TryParseNumber(endText, out end))
            {
                errors.Add($"'{endText}' is not a valid page number.");
                continue;
            }

            if (end < start)
            {
                errors.Add($"'{token}' is not a valid range.");
                continue;
            }

            if (start > pageCount)
            {
                errors.Add($"Page {start} is outside the document (1-{pageCount}).");
                continue;
            }

            end = Math.Min(end, pageCount);
            for (int page = start; page <= end; page++)
            {
                selected.Add(page);
            }
        }

        if (errors.Count > 0)
        {
            error = string.Join(" ", errors);
            return false;
        }

        if (selected.Count == 0)
        {
            error = "The page range does not select any page.";
            return false;
        }

        pages = selected.ToArray();
        error = null;
        return true;
    }

    /// <summary>Formats a list of one based page numbers as a compact range expression.</summary>
    public static string Format(IEnumerable<int> pages)
    {
        if (pages is null || !pages.Any())
        {
            return string.Empty;
        }

        var ordered = pages.Distinct().OrderBy(p => p).ToArray();
        var builder = new StringBuilder();
        int rangeStart = ordered[0];
        int previous = ordered[0];

        for (int i = 1; i <= ordered.Length; i++)
        {
            if (i < ordered.Length && ordered[i] == previous + 1)
            {
                previous = ordered[i];
                continue;
            }

            if (builder.Length > 0)
            {
                builder.Append(", ");
            }

            if (rangeStart == previous)
            {
                builder.Append(rangeStart.ToString(CultureInfo.InvariantCulture));
            }
            else
            {
                builder.Append(rangeStart.ToString(CultureInfo.InvariantCulture));
                builder.Append('-');
                builder.Append(previous.ToString(CultureInfo.InvariantCulture));
            }

            if (i < ordered.Length)
            {
                rangeStart = ordered[i];
                previous = ordered[i];
            }
        }

        return builder.ToString();
    }

    private static bool TryParseNumber(string text, out int value) =>
        int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
}
