using System.Text;
using MarkdownToPdf.Core.Document;

namespace MarkdownToPdf.Markdown;

internal static class InlineMarkdownParser
{
    public static List<InlineRun> Parse(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException("Inline text must be non-empty.", nameof(text));
        }

        var runs = new List<InlineRun>();
        var buffer = new StringBuilder();
        var i = 0;

        while (i < text.Length)
        {
            if (StartsWith(text, i, "**"))
            {
                var end = text.IndexOf("**", i + 2, StringComparison.Ordinal);
                if (end > i + 2)
                {
                    FlushBuffer(buffer, runs);
                    var boldText = text[(i + 2)..end];
                    runs.Add(new InlineRun(boldText, TextStyle.Bold));
                    i = end + 2;
                    continue;
                }
            }

            if (text[i] == '*')
            {
                var end = text.IndexOf('*', i + 1);
                if (end > i + 1)
                {
                    FlushBuffer(buffer, runs);
                    var italicText = text[(i + 1)..end];
                    runs.Add(new InlineRun(italicText, TextStyle.Italic));
                    i = end + 1;
                    continue;
                }
            }

            if (text[i] == '`')
            {
                var runLength = 1;
                while (i + runLength < text.Length && text[i + runLength] == '`')
                {
                    runLength++;
                }

                var end = FindClosingBackticks(text, i + runLength, runLength);
                if (end > i + runLength)
                {
                    FlushBuffer(buffer, runs);
                    var codeText = text[(i + runLength)..end];
                    runs.Add(new InlineRun(codeText, TextStyle.Monospace));
                    i = end + runLength;
                    continue;
                }
            }

            buffer.Append(text[i]);
            i++;
        }

        FlushBuffer(buffer, runs);
        return runs;
    }

    private static void FlushBuffer(StringBuilder buffer, List<InlineRun> runs)
    {
        if (buffer.Length == 0)
        {
            return;
        }

        runs.Add(new InlineRun(buffer.ToString(), TextStyle.Regular));
        buffer.Clear();
    }

    private static bool StartsWith(string text, int index, string value)
    {
        if (index + value.Length > text.Length)
        {
            return false;
        }

        return text.AsSpan(index).StartsWith(value, StringComparison.Ordinal);
    }

    private static int FindClosingBackticks(string text, int startIndex, int runLength)
    {
        var i = startIndex;
        while (i <= text.Length - runLength)
        {
            var match = true;
            for (var j = 0; j < runLength; j++)
            {
                if (text[i + j] != '`')
                {
                    match = false;
                    break;
                }
            }

            if (match)
            {
                return i;
            }

            i++;
        }

        return -1;
    }
}
