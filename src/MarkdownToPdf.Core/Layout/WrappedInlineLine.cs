using MarkdownToPdf.Core.Document;

namespace MarkdownToPdf.Core.Layout;

internal sealed partial class TextLayoutComponent
{
    internal readonly record struct WrappedInlineLine(IReadOnlyList<InlineRun> Inlines, double Width);
}
