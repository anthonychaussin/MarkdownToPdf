namespace MarkdownToPdf.Core.Document;

public sealed class Table : IDocumentElement
{
    public IReadOnlyList<TableRow> Rows { get; }
    public TableBorderStyle BorderStyle { get; }
    public double BorderThickness { get; }
    public double CellPadding { get; }
    public IReadOnlyList<TableColumnAlignment> ColumnAlignments { get; }
    public TableCellVerticalAlignment DefaultCellVerticalAlignment { get; }

    public Table(
        IReadOnlyList<TableRow> rows,
        TableBorderStyle borderStyle = TableBorderStyle.None,
        double borderThickness = 0.5,
        IReadOnlyList<TableColumnAlignment>? columnAlignments = null,
        double cellPadding = 8.0,
        TableCellVerticalAlignment defaultCellVerticalAlignment = TableCellVerticalAlignment.Top)
    {
        ArgumentNullException.ThrowIfNull(rows);
        if (rows.Count == 0)
        {
            throw new ArgumentException("Table must contain at least one row.", nameof(rows));
        }

        Rows = [.. rows];
        BorderStyle = borderStyle;
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(borderThickness);
        BorderThickness = borderThickness;
        ArgumentOutOfRangeException.ThrowIfNegative(cellPadding);
        CellPadding = cellPadding;
        ColumnAlignments = columnAlignments is null ? [] : [.. columnAlignments];
        DefaultCellVerticalAlignment = defaultCellVerticalAlignment;
    }
}
