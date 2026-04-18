namespace MarkdownToPdf.Core.Layout;

internal abstract class LayoutItem(double x, double y)
{
    public double X { get; } = x;
    public double Y { get; } = y;
}
