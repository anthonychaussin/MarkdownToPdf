namespace MarkdownToPdf.Core.Document;

public sealed class TableRow
{
    public IReadOnlyList<TableCell> Cells { get; }
    public bool IsHeader { get; }

    public TableRow(IReadOnlyList<TableCell> cells, bool isHeader = false)
    {
        ArgumentNullException.ThrowIfNull(cells);
        if (cells.Count == 0)
        {
            throw new ArgumentException("Table row must contain at least one cell.", nameof(cells));
        }

        Cells = [.. cells];
        IsHeader = isHeader;
    }
}
