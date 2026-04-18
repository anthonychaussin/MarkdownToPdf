using MarkdownToPdf.Fonts;

namespace MarkdownToPdf.Tests;

public sealed class PdfFontTests
{
    [Fact]
    public void FromTrueTypeFile_InvalidBytes_ThrowsInvalidDataException()
    {
        var path = Path.Combine(Path.GetTempPath(), $"MarkdownToPdf-invalid-font-{Guid.NewGuid():N}.ttf");
        File.WriteAllBytes(path, [1, 2, 3, 4, 5]);

        try
        {
            Assert.Throws<InvalidDataException>(() => PdfFont.FromTrueTypeFile(path));
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}
