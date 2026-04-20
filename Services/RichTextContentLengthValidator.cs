using System.Net;
using System.Text.RegularExpressions;

namespace SmartELibrary.Services;

public readonly record struct RichTextLengthMetrics(int PlainTextLength, int EstimatedLines, int RawLines);

public static class RichTextContentLengthValidator
{
    public const int MaxLines = 60;

    // Used to approximate "60 lines" for rich text that has few explicit newlines.
    public const int CharsPerLineForEstimation = 80;

    // Equivalent length threshold used alongside line estimation.
    public const int MaxPlainTextChars = MaxLines * CharsPerLineForEstimation;

    public const string TooLongMessage = "Content is too long. Please create a new page.";

    public static bool IsWithinLimit(string? htmlContent, out RichTextLengthMetrics metrics)
    {
        metrics = Estimate(htmlContent);

        if (metrics.PlainTextLength <= 0)
        {
            return true;
        }

        return metrics.EstimatedLines <= MaxLines && metrics.PlainTextLength <= MaxPlainTextChars;
    }

    public static RichTextLengthMetrics Estimate(string? htmlContent)
    {
        var plain = ExtractPlainTextPreservingBreaks(htmlContent);
        if (string.IsNullOrWhiteSpace(plain))
        {
            return new RichTextLengthMetrics(0, 0, 0);
        }

        plain = NormalizeWhitespace(plain);

        var rawLines = CountRawLines(plain);
        var wrapLines = (int)Math.Ceiling(plain.Length / (double)CharsPerLineForEstimation);
        var estimatedLines = Math.Max(rawLines, wrapLines);

        return new RichTextLengthMetrics(plain.Length, estimatedLines, rawLines);
    }

    // Helper method required by spec
    public static int EstimateLineCountFromRichText(string? htmlContent)
    {
        return Estimate(htmlContent).EstimatedLines;
    }

    private static int CountRawLines(string text)
    {
        var normalized = text.Replace("\r\n", "\n").Replace('\r', '\n');
        normalized = normalized.TrimEnd('\n');
        if (normalized.Length == 0)
        {
            return 0;
        }

        return normalized.Split('\n').Length;
    }

    private static string NormalizeWhitespace(string text)
    {
        var normalized = text.Replace("\r\n", "\n").Replace('\r', '\n');

        // Collapse horizontal whitespace but keep line breaks.
        normalized = Regex.Replace(normalized, "[\\t\\f\\v ]+", " ");
        normalized = Regex.Replace(normalized, " *\\n *", "\n");

        return normalized.Trim();
    }

    private static string ExtractPlainTextPreservingBreaks(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return string.Empty;
        }

        var value = html;

        // Add line breaks for common block elements so line counting is meaningful.
        value = Regex.Replace(value, "(?i)<br\\s*/?>", "\n");
        value = Regex.Replace(value, "(?i)</p\\s*>", "\n");
        value = Regex.Replace(value, "(?i)</div\\s*>", "\n");
        value = Regex.Replace(value, "(?i)</li\\s*>", "\n");
        value = Regex.Replace(value, "(?i)</h[1-6]\\s*>", "\n");
        value = Regex.Replace(value, "(?i)</tr\\s*>", "\n");

        // Remove remaining tags.
        value = Regex.Replace(value, "<[^>]+>", " ");

        // Decode entities (&nbsp; etc.).
        value = WebUtility.HtmlDecode(value);

        // Treat non-breaking spaces as regular spaces.
        value = value.Replace('\u00A0', ' ');

        return value;
    }
}
