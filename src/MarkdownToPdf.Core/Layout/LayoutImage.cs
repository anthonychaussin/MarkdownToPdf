namespace MarkdownToPdf.Core.Layout;

internal sealed class LayoutImage : LayoutItem
{
    public LayoutImage(
        double x,
        double topY,
        double width,
        double height,
        int pixelWidth,
        int pixelHeight,
        byte[] rgbData)
        : base(x, topY)
    {
        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width), "Image width must be positive.");
        }

        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height), "Image height must be positive.");
        }

        if (pixelWidth <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(pixelWidth), "Image pixel width must be positive.");
        }

        if (pixelHeight <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(pixelHeight), "Image pixel height must be positive.");
        }

        if (rgbData is null || rgbData.Length == 0)
        {
            throw new ArgumentException("Image data must be non-empty.", nameof(rgbData));
        }

        Width = width;
        Height = height;
        PixelWidth = pixelWidth;
        PixelHeight = pixelHeight;
        RgbData = rgbData;
    }

    public double Width { get; }
    public double Height { get; }
    public int PixelWidth { get; }
    public int PixelHeight { get; }
    public byte[] RgbData { get; }
}
