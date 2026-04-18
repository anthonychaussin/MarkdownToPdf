namespace MarkdownToPdf.Core.Layout;

internal sealed class LayoutCheckbox(double x, double y, double size, double thickness, bool isChecked) : LayoutItem(x, y)
{
    public double Size { get; } = size;
    public double Thickness { get; } = thickness;
    public bool IsChecked { get; } = isChecked;
}