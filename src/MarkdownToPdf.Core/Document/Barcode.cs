namespace MarkdownToPdf.Core.Document;

public enum BarcodeKind
{
    Code128
}

public sealed class Barcode : IDocumentElement
{
    public const double DefaultWidth = 180;
    public const double DefaultHeight = 60;

    public string Content { get; }
    public BarcodeKind Kind { get; }
    public double Width { get; }
    public double Height { get; }

    public Barcode(
        string content,
        BarcodeKind kind = BarcodeKind.Code128,
        double width = DefaultWidth,
        double height = DefaultHeight)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new ArgumentException("Barcode content must be non-empty.", nameof(content));
        }

        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width), "Barcode width must be positive.");
        }

        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height), "Barcode height must be positive.");
        }

        Content = content;
        Kind = kind;
        Width = width;
        Height = height;
    }
}
