namespace MarkdownToPdf.Core.Layout;

internal sealed partial class TableLayoutComponent
{
    private readonly record struct TableLayoutPlan(
        double[] ColumnWidths,
        IReadOnlyList<TableRowLayout> RowLayouts,
        double TotalWidth,
        double LineHeight,
        double RequiredHeight,
        double FontSize);
}
