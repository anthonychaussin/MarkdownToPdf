using MarkdownToPdf.Core.Document;

namespace MarkdownToPdf.Core.Layout;

internal sealed partial class TableLayoutComponent
{
    private readonly record struct PreparedCellRun(string Text, TextStyle Style, double Width);
}
