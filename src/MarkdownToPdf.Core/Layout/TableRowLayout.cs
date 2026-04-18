using MarkdownToPdf.Core.Document;

namespace MarkdownToPdf.Core.Layout;

internal sealed partial class TableLayoutComponent
{
    private readonly record struct TableRowLayout(
        TableRow Row,
        List<IReadOnlyList<PreparedCellLine>> Lines,
        double[] MaxContentWidths,
        int MaxLines,
        double Height);
}
