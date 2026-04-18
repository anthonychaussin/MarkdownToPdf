using MarkdownToPdf.Core.Document;

namespace MarkdownToPdf.Markdown;

internal static class MarkdownTableParser
{
    public static bool IsTableHeader(string[] lines, int index)
    {
        if (index + 1 >= lines.Length)
        {
            return false;
        }

        var header = lines[index];
        var separator = lines[index + 1];
        return IsTableLine(header) && IsTableSeparator(separator);
    }

    public static bool IsTableLine(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return false;
        }

        return line.Contains('|');
    }

    public static Table ParseTable(
        string[] lines,
        ref int index,
        TableBorderStyle borderStyle,
        double borderThickness,
        double cellPadding,
        TableCellVerticalAlignment defaultVerticalAlignment)
    {
        var rows = new List<TableRow>();
        var headerCellTexts = SplitTableLine(lines[index]);
        var headerCells = new List<TableCell>(headerCellTexts.Count);
        foreach (var cellText in headerCellTexts)
        {
            headerCells.Add(new TableCell(ParseTableCellInlines(cellText)));
        }

        var alignments = ParseTableAlignments(lines[index + 1], headerCells.Count);
        rows.Add(new TableRow(headerCells, isHeader: true));
        index += 2;

        while (index < lines.Length && IsTableLine(lines[index]))
        {
            var cellTexts = SplitTableLine(lines[index]);
            var cells = new List<TableCell>(cellTexts.Count);
            foreach (var cellText in cellTexts)
            {
                cells.Add(new TableCell(ParseTableCellInlines(cellText)));
            }

            rows.Add(new TableRow(cells));
            index++;
        }

        return new Table(rows, borderStyle, borderThickness, alignments, cellPadding, defaultVerticalAlignment);
    }

    private static bool IsTableSeparator(string line)
    {
        var cells = SplitTableLine(line);
        if (cells.Count == 0)
        {
            return false;
        }

        foreach (var cell in cells)
        {
            var trimmed = cell.Trim();
            if (trimmed.Length < 3)
            {
                return false;
            }

            foreach (var ch in trimmed)
            {
                if (ch != '-' && ch != ':' && ch != ' ')
                {
                    return false;
                }
            }
        }

        return true;
    }

    private static List<string> SplitTableLine(string line)
    {
        var trimmed = line.Trim();
        if (trimmed.StartsWith('|'))
        {
            trimmed = trimmed[1..];
        }

        if (trimmed.EndsWith('|'))
        {
            trimmed = trimmed[..^1];
        }

        var cells = trimmed.Split('|', StringSplitOptions.TrimEntries);
        return [.. cells];
    }

    private static List<TableColumnAlignment> ParseTableAlignments(string separatorLine, int columnCount)
    {
        var cells = SplitTableLine(separatorLine);
        var alignments = new List<TableColumnAlignment>(columnCount);

        for (var i = 0; i < columnCount; i++)
        {
            if (i >= cells.Count)
            {
                alignments.Add(TableColumnAlignment.Left);
                continue;
            }

            var trimmed = cells[i];
            var startsWithColon = trimmed.StartsWith(':');
            var endsWithColon = trimmed.EndsWith(':');

            if (startsWithColon && endsWithColon)
            {
                alignments.Add(TableColumnAlignment.Center);
            }
            else if (endsWithColon)
            {
                alignments.Add(TableColumnAlignment.Right);
            }
            else
            {
                alignments.Add(TableColumnAlignment.Left);
            }
        }

        return alignments;
    }

    private static List<InlineRun> ParseTableCellInlines(string cell)
    {
        var text = cell.Trim();
        if (text.Length == 0)
        {
            return [new InlineRun(" ")];
        }

        return InlineMarkdownParser.Parse(text);
    }
}
