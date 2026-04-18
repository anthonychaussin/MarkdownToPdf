using MarkdownToPdf.Core.Document;

namespace MarkdownToPdf.Core.Layout;

internal sealed partial class TextLayoutComponent
{
    private sealed class ParagraphLayoutState(double fontSize, ParagraphAlignment alignment)
    {
        public double FontSize { get; } = fontSize;
        public double LineHeight { get; } = fontSize * 1.2;
        public ParagraphAlignment Alignment { get; } = alignment;
    }
}
