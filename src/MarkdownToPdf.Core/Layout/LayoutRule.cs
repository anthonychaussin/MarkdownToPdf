namespace MarkdownToPdf.Core.Layout;

internal sealed class LayoutRule(double x, double y, double width, double thickness) : LayoutItem(x, y)
{
    public double Width { get; } = width;
    public double Thickness { get; } = thickness;
}
