using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace MarkdownToPdf.Core.Document;

public sealed class PdfImage : IDocumentElement
{
    public int PixelWidth { get; }
    public int PixelHeight { get; }
    public byte[] RgbData { get; }
    public double Width { get; }
    public double Height { get; }

    public PdfImage(
        int pixelWidth,
        int pixelHeight,
        byte[] rgbData,
        double? width = null,
        double? height = null)
    {
        if (pixelWidth <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(pixelWidth), "Image pixel width must be positive.");
        }

        if (pixelHeight <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(pixelHeight), "Image pixel height must be positive.");
        }

        ArgumentNullException.ThrowIfNull(rgbData);

        var expectedLength = checked(pixelWidth * pixelHeight * 3);
        if (rgbData.Length != expectedLength)
        {
            throw new ArgumentException(
                $"RGB data length must equal pixelWidth * pixelHeight * 3 ({expectedLength}).",
                nameof(rgbData));
        }

        var (resolvedWidth, resolvedHeight) = ResolveDimensions(pixelWidth, pixelHeight, width, height);

        PixelWidth = pixelWidth;
        PixelHeight = pixelHeight;
        RgbData = [.. rgbData];
        Width = resolvedWidth;
        Height = resolvedHeight;
    }

    public static PdfImage FromFile(string path, double? width = null, double? height = null)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Image path must be non-empty.", nameof(path));
        }

        var encoded = File.ReadAllBytes(path);
        return FromBytes(encoded, width, height);
    }

    public static PdfImage FromBytes(byte[] encodedBytes, double? width = null, double? height = null)
    {
        ArgumentNullException.ThrowIfNull(encodedBytes);
        if (encodedBytes.Length == 0)
        {
            throw new ArgumentException("Encoded image bytes must be non-empty.", nameof(encodedBytes));
        }

        using var image = Image.Load<Rgb24>(encodedBytes);
        var rgbData = new byte[checked(image.Width * image.Height * 3)];
        image.CopyPixelDataTo(rgbData);

        return new PdfImage(image.Width, image.Height, rgbData, width, height);
    }

    private static (double Width, double Height) ResolveDimensions(
        int pixelWidth,
        int pixelHeight,
        double? width,
        double? height)
    {
        if (width is <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width), "Image width must be positive.");
        }

        if (height is <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height), "Image height must be positive.");
        }

        if (width.HasValue && height.HasValue)
        {
            return (width.Value, height.Value);
        }

        if (width.HasValue)
        {
            return (width.Value, width.Value * ((double)pixelHeight / pixelWidth));
        }

        if (height.HasValue)
        {
            return (height.Value * ((double)pixelWidth / pixelHeight), height.Value);
        }

        return (pixelWidth, pixelHeight);
    }
}
