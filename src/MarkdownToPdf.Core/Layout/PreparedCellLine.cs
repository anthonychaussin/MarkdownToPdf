namespace MarkdownToPdf.Core.Layout;

internal sealed partial class TableLayoutComponent
{
    private readonly record struct PreparedCellLine(IReadOnlyList<PreparedCellRun> Runs, double Width);
}
