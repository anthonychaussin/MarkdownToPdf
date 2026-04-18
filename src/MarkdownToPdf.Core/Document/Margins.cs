namespace MarkdownToPdf.Core.Document;

public readonly struct Margins
{
    public double Left { get; }
    public double Top { get; }
    public double Right { get; }
    public double Bottom { get; }

    public Margins(double left, double top, double right, double bottom)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(left);
        ArgumentOutOfRangeException.ThrowIfNegative(top);
        ArgumentOutOfRangeException.ThrowIfNegative(right);
        ArgumentOutOfRangeException.ThrowIfNegative(bottom);

        Left = left;
        Top = top;
        Right = right;
        Bottom = bottom;
    }

    public static Margins Uniform(double value)
    {
        return new Margins(value, value, value, value);
    }
}
