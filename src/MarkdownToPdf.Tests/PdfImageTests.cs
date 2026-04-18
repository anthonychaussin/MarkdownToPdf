using MarkdownToPdf.Core.Document;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;

namespace MarkdownToPdf.Tests;

public sealed class PdfImageTests
{
    [Fact]
    public void Constructor_Validates_Rgb_Length()
    {
        var ex = Assert.Throws<ArgumentException>(() => new PdfImage(2, 2, new byte[3]));
        Assert.Contains("RGB data length", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void FromBytes_Creates_Image_With_Default_Size()
    {
        using var source = new Image<Rgb24>(1, 1);
        source[0, 0] = new Rgb24(255, 0, 0);
        using var stream = new MemoryStream();
        source.Save(stream, new PngEncoder());

        var image = PdfImage.FromBytes(stream.ToArray());

        Assert.Equal(1, image.PixelWidth);
        Assert.Equal(1, image.PixelHeight);
        Assert.Equal(1, image.Width);
        Assert.Equal(1, image.Height);
        Assert.Equal(3, image.RgbData.Length);
    }

    [Fact]
    public void FromBytes_Computes_Missing_Dimension_With_Aspect_Ratio()
    {
        var rgb = new byte[2 * 1 * 3];
        var image = new PdfImage(2, 1, rgb, width: 100);

        Assert.Equal(100, image.Width);
        Assert.Equal(50, image.Height);
    }
}
