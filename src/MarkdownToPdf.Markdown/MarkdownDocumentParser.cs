using System.IO;
using System.Text;
using MarkdownToPdf.Core.Document;
using MarkdownToPdf.Markdown.Application.Abstractions;

namespace MarkdownToPdf.Markdown;

public sealed class MarkdownDocumentParser : IMarkdownDocumentParser
{
    public PdfDocument Parse(string markdown, PdfTheme? theme = null)
    {
        ArgumentNullException.ThrowIfNull(markdown);

        var elements = new List<IDocumentElement>();
        var lines = markdown.Replace("\r\n", "\n").Split('\n');
        var index = 0;
        TableBorderStyle? pendingTableBorder = null;
        double? pendingBorderThickness = null;
        double? pendingCellPadding = null;
        TableCellVerticalAlignment? pendingVerticalAlignment = null;
        ParagraphAlignment? pendingParagraphAlignment = null;

        while (index < lines.Length)
        {
            var line = lines[index];
            if (string.IsNullOrWhiteSpace(line))
            {
                index++;
                continue;
            }

            if (IsHorizontalRule(line))
            {
                elements.Add(new HorizontalRule());
                index++;
                continue;
            }

            if (MarkdownDirectiveParser.TryParseParagraphAlignment(line, out var paragraphAlignment))
            {
                pendingParagraphAlignment = paragraphAlignment;
                index++;
                continue;
            }

            if (MarkdownDirectiveParser.IsPageBreak(line))
            {
                elements.Add(new PageBreak());
                index++;
                continue;
            }

            if (MarkdownDirectiveParser.TryParseTableDirective(line, out var borderStyle, out var borderThickness, out var cellPadding, out var verticalAlignment))
            {
                pendingTableBorder = borderStyle;
                pendingBorderThickness = borderThickness;
                pendingCellPadding = cellPadding;
                pendingVerticalAlignment = verticalAlignment;
                index++;
                continue;
            }

            if (MarkdownDirectiveParser.TryParseQrCodeLine(line, theme, out var qrCode))
            {
                elements.Add(qrCode);
                index++;
                continue;
            }

            if (MarkdownDirectiveParser.TryParseBarcodeLine(line, out var barcode))
            {
                elements.Add(barcode);
                index++;
                continue;
            }

            if (IsCodeFence(line))
            {
                index++;
                while (index < lines.Length && !IsCodeFence(lines[index]))
                {
                    var codeLine = lines[index];
                    if (!string.IsNullOrEmpty(codeLine))
                    {
                        elements.Add(new Paragraph([new InlineRun(codeLine, TextStyle.Monospace)]));
                    }
                    index++;
                }

                if (index < lines.Length && IsCodeFence(lines[index]))
                {
                    index++;
                }

                continue;
            }

            if (TryParseHeading(line, out var heading))
            {
                elements.Add(heading);
                index++;
                continue;
            }

            if (IsBlockQuote(line))
            {
                var quoteLines = new List<string>();
                var quoteHardBreaks = new List<bool>();
                while (index < lines.Length && IsBlockQuote(lines[index]))
                {
                    var raw = StripBlockQuotePrefix(lines[index]);
                    var trimmed = raw.TrimEnd();
                    var hasHardBreak = trimmed.EndsWith("  ", StringComparison.Ordinal) || trimmed.EndsWith('\\');
                    if (hasHardBreak)
                    {
                        trimmed = trimmed.TrimEnd(' ', '\\');
                    }

                    quoteLines.Add(trimmed.Trim());
                    quoteHardBreaks.Add(hasHardBreak);
                    index++;
                }

                var quoteText = BuildParagraphText(quoteLines, quoteHardBreaks);
                elements.Add(new BlockQuote(InlineMarkdownParser.Parse(quoteText)));
                continue;
            }

            if (IsTaskListItem(line))
            {
                var items = new List<TaskItem>();
                while (index < lines.Length && IsTaskListItem(lines[index]))
                {
                    var (isChecked, itemText) = ParseTaskListItem(lines[index]);
                    var continuation = new List<string>();
                    index++;

                    while (index < lines.Length)
                    {
                        var next = lines[index];
                        if (string.IsNullOrWhiteSpace(next))
                        {
                            break;
                        }

                        if (IsTaskListItem(next) || IsListItem(next))
                        {
                            break;
                        }

                        if (IsIndentedContinuation(next))
                        {
                            continuation.Add(next.Trim());
                            index++;
                            continue;
                        }

                        break;
                    }

                    itemText = JoinWithSpaces(itemText, continuation);

                    var inlines = InlineMarkdownParser.Parse(itemText);
                    items.Add(new TaskItem(inlines, isChecked));
                }

                elements.Add(new TaskList(items));
                continue;
            }

            if (IsListItem(line))
            {
                var items = new List<ListItem>();
                while (index < lines.Length && IsListItem(lines[index]))
                {
                    var itemText = lines[index].TrimStart()[2..];
                    var continuation = new List<string>();
                    index++;

                    while (index < lines.Length)
                    {
                        var next = lines[index];
                        if (string.IsNullOrWhiteSpace(next))
                        {
                            break;
                        }

                        if (IsListItem(next) || IsTaskListItem(next))
                        {
                            break;
                        }

                        if (IsIndentedContinuation(next))
                        {
                            continuation.Add(next.Trim());
                            index++;
                            continue;
                        }

                        break;
                    }

                    itemText = JoinWithSpaces(itemText, continuation);

                    var inlines = InlineMarkdownParser.Parse(itemText);
                    items.Add(new ListItem(inlines));
                }

                elements.Add(new BulletList(items));
                continue;
            }

            if (MarkdownTableParser.IsTableHeader(lines, index))
            {
                var table = MarkdownTableParser.ParseTable(
                    lines,
                    ref index,
                    pendingTableBorder ?? TableBorderStyle.None,
                    pendingBorderThickness ?? 0.5,
                    pendingCellPadding ?? 8.0,
                    pendingVerticalAlignment ?? TableCellVerticalAlignment.Top);
                pendingTableBorder = null;
                pendingBorderThickness = null;
                pendingCellPadding = null;
                pendingVerticalAlignment = null;
                elements.Add(table);
                continue;
            }

            var paragraphLines = new List<string>();
            var hardBreaks = new List<bool>();
            while (index < lines.Length && !string.IsNullOrWhiteSpace(lines[index]) && !IsSpecialLine(lines[index], theme))
            {
                var rawLine = lines[index];
                var trimmed = rawLine.TrimEnd();
                var hasHardBreak = trimmed.EndsWith("  ", StringComparison.Ordinal) || trimmed.EndsWith('\\');
                if (hasHardBreak)
                {
                    trimmed = trimmed.TrimEnd(' ', '\\');
                }

                paragraphLines.Add(trimmed.Trim());
                hardBreaks.Add(hasHardBreak);
                index++;
            }

            if (paragraphLines.Count > 0)
            {
                var paragraphText = BuildParagraphText(paragraphLines, hardBreaks);
                var alignment = pendingParagraphAlignment ?? ParagraphAlignment.Left;
                elements.Add(new Paragraph(InlineMarkdownParser.Parse(paragraphText), alignment));
                pendingParagraphAlignment = null;
            }
        }

        if (elements.Count == 0)
        {
            throw new ArgumentException("Markdown content produced no document elements.", nameof(markdown));
        }

        return new PdfDocument(elements, theme);
    }

    private static bool TryParseHeading(string line, out Heading heading)
    {
        heading = null!;
        var trimmed = line.TrimStart();
        var level = 0;
        while (level < trimmed.Length && trimmed[level] == '#')
        {
            level++;
        }

        if (level == 0 || level > 6 || level >= trimmed.Length || trimmed[level] != ' ')
        {
            return false;
        }

        var text = trimmed[(level + 1)..].Trim();
        heading = new Heading(level, InlineMarkdownParser.Parse(text));
        return true;
    }

    private static bool IsListItem(string line)
    {
        var trimmed = line.TrimStart();
        return trimmed.StartsWith("- ") || trimmed.StartsWith("* ") || trimmed.StartsWith("+ ");
    }

    private static bool IsTaskListItem(string line)
    {
        var trimmed = line.TrimStart();
        return trimmed.StartsWith("- [ ] ", StringComparison.Ordinal)
               || trimmed.StartsWith("- [x] ", StringComparison.OrdinalIgnoreCase);
    }

    private static (bool IsChecked, string Text) ParseTaskListItem(string line)
    {
        var trimmed = line.TrimStart();
        var isChecked = trimmed.StartsWith("- [x] ", StringComparison.OrdinalIgnoreCase);
        var text = trimmed.Length > 6 ? trimmed[6..].Trim() : string.Empty;
        return (isChecked, text);
    }

    private static string BuildParagraphText(List<string> lines, List<bool> hardBreaks)
    {
        if (lines.Count == 0)
        {
            return string.Empty;
        }

        var builder = new StringBuilder();
        for (var i = 0; i < lines.Count; i++)
        {
            if (i > 0)
            {
                builder.Append(hardBreaks[i - 1] ? "\n" : " ");
            }

            builder.Append(lines[i]);
        }

        return builder.ToString();
    }

    private static bool IsIndentedContinuation(string line)
    {
        return line.StartsWith("  ", StringComparison.Ordinal) || line.StartsWith('\t');
    }

    private static bool IsHorizontalRule(string line)
    {
        var trimmed = line.Trim();
        if (trimmed.Length < 3)
        {
            return false;
        }

        var ch = trimmed[0];
        if (ch != '-' && ch != '*' && ch != '_')
        {
            return false;
        }

        for (var i = 0; i < trimmed.Length; i++)
        {
            if (trimmed[i] != ch)
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsCodeFence(string line)
    {
        return line.Trim() == "```";
    }

    private static bool IsSpecialLine(string line, PdfTheme? theme)
    {
        return TryParseHeading(line, out _)
               || IsListItem(line)
               || IsTaskListItem(line)
               || IsBlockQuote(line)
               || IsHorizontalRule(line)
               || MarkdownDirectiveParser.TryParseParagraphAlignment(line, out _)
               || MarkdownTableParser.IsTableLine(line)
               || MarkdownDirectiveParser.IsPageBreak(line)
               || MarkdownDirectiveParser.TryParseTableDirective(line, out _, out _, out _, out _)
               || IsCodeFence(line)
               || MarkdownDirectiveParser.TryParseQrCodeLine(line, theme, out _)
               || MarkdownDirectiveParser.TryParseBarcodeLine(line, out _);
    }

    private static bool IsBlockQuote(string line)
    {
        var trimmed = line.TrimStart();
        return trimmed.StartsWith("> ", StringComparison.Ordinal) || trimmed == ">";
    }

    private static string StripBlockQuotePrefix(string line)
    {
        var trimmed = line.TrimStart();
        if (trimmed.StartsWith("> ", StringComparison.Ordinal))
        {
            return trimmed[2..];
        }

        if (trimmed == ">")
        {
            return string.Empty;
        }

        return trimmed;
    }

    public PdfDocument Parse(Stream markdownStream, PdfTheme? theme = null, Encoding? encoding = null)
    {
        ArgumentNullException.ThrowIfNull(markdownStream);

        using var reader = new StreamReader(markdownStream, encoding ?? Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        var markdown = reader.ReadToEnd();
        return Parse(markdown, theme);
    }

    public PdfDocument ParseFile(string path, PdfTheme? theme = null, Encoding? encoding = null)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Markdown path must be non-empty.", nameof(path));
        }

        using var stream = File.OpenRead(path);
        return Parse(stream, theme, encoding);
    }

    private static string JoinWithSpaces(string first, List<string> continuation)
    {
        if (continuation.Count == 0)
        {
            return first;
        }

        var builder = new StringBuilder(first.Length + 1);
        builder.Append(first);
        foreach (var part in continuation)
        {
            builder.Append(' ').Append(part);
        }

        return builder.ToString();
    }
}
