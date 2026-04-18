namespace MarkdownToPdf.Core.Layout;

internal sealed class LayoutLine(double x1, double y1, double x2, double y2, double thickness) : LayoutItem(x1, y1)
{
    public double X2 { get; } = x2;
    public double Y2 { get; } = y2;
    public double Thickness { get; } = thickness;
}
