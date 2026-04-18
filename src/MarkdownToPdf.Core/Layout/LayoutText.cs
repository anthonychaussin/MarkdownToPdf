namespace MarkdownToPdf.Core.Layout;

internal sealed class LayoutText(string text, double x, double y, double fontSize, Document.TextStyle style) : LayoutItem(x, y)
{
    public string Text { get; } = text ?? throw new ArgumentNullException(nameof(text));
    public double FontSize { get; } = fontSize;
    public Document.TextStyle Style { get; } = style;
}