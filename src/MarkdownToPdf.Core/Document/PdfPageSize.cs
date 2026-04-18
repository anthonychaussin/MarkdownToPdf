namespace MarkdownToPdf.Core.Document;

public readonly struct PdfPageSize
{
    public double Width { get; }
    public double Height { get; }

    public PdfPageSize(double width, double height)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);

        Width = width;
        Height = height;
    }

    public static PdfPageSize A4 => new(595, 842);
}
