namespace MarkdownToPdf.Core.Document;

public sealed class TableCell
{
    public IReadOnlyList<InlineRun> Inlines { get; }
    public TableCellVerticalAlignment? VerticalAlignment { get; }

    public TableCell(IReadOnlyList<InlineRun> inlines, TableCellVerticalAlignment? verticalAlignment = null)
    {
        ArgumentNullException.ThrowIfNull(inlines);
        if (inlines.Count == 0)
        {
            throw new ArgumentException("Table cell must contain at least one inline run.", nameof(inlines));
        }

        Inlines = [.. inlines];
        VerticalAlignment = verticalAlignment;
    }
}
