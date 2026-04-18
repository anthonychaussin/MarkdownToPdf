using System.Text;
using MarkdownToPdf.Core.Document;
using MarkdownToPdf.Core.Rendering;

namespace MarkdownToPdf.Tests;

public sealed class PdfRendererTests
{
    private static byte[] RenderToBytes(PdfRenderer renderer, PdfDocument document, PdfRendererOptions? options = null)
    {
        using var stream = new MemoryStream();
        renderer.Render(document, stream, options);
        return stream.ToArray();
    }

    [Fact]
    public void Render_Writes_Valid_Pdf_Header_And_Text()
    {
        var document = new PdfDocument(new IDocumentElement[]
        {
            Heading.FromText(1, "Title"),
            Paragraph.FromText("Hello, PDF.")
        });

        var renderer = new PdfRenderer();
        var bytes = RenderToBytes(renderer, document);
        var content = Encoding.ASCII.GetString(bytes);

        Assert.StartsWith("%PDF-1.4", content, StringComparison.Ordinal);
        Assert.Contains("(Title)", content, StringComparison.Ordinal);
        Assert.Contains("(Hello,)", content, StringComparison.Ordinal);
        Assert.Contains("(PDF.)", content, StringComparison.Ordinal);
        Assert.Contains("%%EOF", content, StringComparison.Ordinal);
    }

    [Fact]
    public void Render_BulletList_And_Rule_Are_Emitted()
    {
        var document = new PdfDocument(new IDocumentElement[]
        {
            new BulletList(new[]
            {
                ListItem.FromText("First"),
                ListItem.FromText("Second")
            }),
            new HorizontalRule()
        });

        var renderer = new PdfRenderer();
        var bytes = RenderToBytes(renderer, document);
        var content = Encoding.ASCII.GetString(bytes);

        Assert.Contains("(- )", content, StringComparison.Ordinal);
        Assert.Contains("(First)", content, StringComparison.Ordinal);
        Assert.Contains("(Second)", content, StringComparison.Ordinal);
        Assert.Contains(" l S", content, StringComparison.Ordinal);
    }

    [Fact]
    public void Render_All_TextStyles_Are_Emitted()
    {
        var document = new PdfDocument(new IDocumentElement[]
        {
            new Paragraph(new[]
            {
                new InlineRun("Regular ", TextStyle.Regular),
                new InlineRun("Bold ", TextStyle.Bold),
                new InlineRun("Italic ", TextStyle.Italic),
                new InlineRun("Code", TextStyle.Monospace)
            })
        });

        var renderer = new PdfRenderer();
        var bytes = RenderToBytes(renderer, document);
        var content = Encoding.ASCII.GetString(bytes);

        Assert.Contains("(Regular)", content, StringComparison.Ordinal);
        Assert.Contains("(Bold)", content, StringComparison.Ordinal);
        Assert.Contains("(Italic)", content, StringComparison.Ordinal);
        Assert.Contains("(Code)", content, StringComparison.Ordinal);
    }

    [Fact]
    public void Render_TaskList_Renders_Checkboxes()
    {
        var document = new PdfDocument(new IDocumentElement[]
        {
            new TaskList(new[]
            {
                new TaskItem(new List<InlineRun> { new InlineRun("First", TextStyle.Regular) }, false),
                new TaskItem(new List<InlineRun> { new InlineRun("Second", TextStyle.Regular) }, true)
            })
        });

        var renderer = new PdfRenderer();
        var bytes = RenderToBytes(renderer, document);
        var content = Encoding.ASCII.GetString(bytes);

        Assert.Contains("(First)", content, StringComparison.Ordinal);
        Assert.Contains("(Second)", content, StringComparison.Ordinal);
        Assert.Contains(" re S", content, StringComparison.Ordinal);
        Assert.DoesNotContain("[ ]", content, StringComparison.Ordinal);
        Assert.DoesNotContain("[x]", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Render_PdfImage_Emits_Image_XObject()
    {
        var image = new PdfImage(
            pixelWidth: 1,
            pixelHeight: 1,
            rgbData: new byte[] { 255, 0, 0 },
            width: 24,
            height: 24);

        var document = new PdfDocument(new IDocumentElement[]
        {
            image
        });

        var renderer = new PdfRenderer();
        var bytes = RenderToBytes(renderer, document);
        var content = Encoding.ASCII.GetString(bytes);

        Assert.Contains("/Subtype /Image", content, StringComparison.Ordinal);
        Assert.Contains("/XObject", content, StringComparison.Ordinal);
        Assert.Contains(" Do Q", content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RenderAsync_To_MemoryStream_Writes_Valid_Pdf_Header_And_Text()
    {
        var document = new PdfDocument(new IDocumentElement[]
        {
            Heading.FromText(1, "Async"),
            Paragraph.FromText("Hello async PDF.")
        });

        var renderer = new PdfRenderer();
        using var stream = new MemoryStream();
        await renderer.RenderAsync(document, stream);
        var content = Encoding.ASCII.GetString(stream.ToArray());

        Assert.StartsWith("%PDF-1.4", content, StringComparison.Ordinal);
        Assert.Contains("(Async)", content, StringComparison.Ordinal);
        Assert.Contains("(Hello)", content, StringComparison.Ordinal);
        Assert.Contains("(async)", content, StringComparison.Ordinal);
    }

    [Fact]
    public void Render_To_File_Path_Writes_Valid_Pdf()
    {
        var document = new PdfDocument(new IDocumentElement[]
        {
            Heading.FromText(1, "FileTitle"),
            Paragraph.FromText("Hello file path.")
        });

        var renderer = new PdfRenderer();
        var filePath = Path.Combine(Path.GetTempPath(), $"MarkdownToPdf-test-{Guid.NewGuid():N}.pdf");
        try
        {
            renderer.Render(document, filePath);

            Assert.True(File.Exists(filePath));
            var content = Encoding.ASCII.GetString(File.ReadAllBytes(filePath));
            Assert.StartsWith("%PDF-1.4", content, StringComparison.Ordinal);
            Assert.Contains("(FileTitle)", content, StringComparison.Ordinal);
            Assert.Contains("%%EOF", content, StringComparison.Ordinal);
        }
        finally
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
    }

    [Fact]
    public void Render_With_Null_Document_Throws()
    {
        var renderer = new PdfRenderer();
        using var stream = new MemoryStream();
        Assert.Throws<ArgumentNullException>(() => renderer.Render(null!, stream));
    }

    [Fact]
    public void Render_With_Null_Stream_Throws()
    {
        var renderer = new PdfRenderer();
        var document = new PdfDocument(new IDocumentElement[] { Paragraph.FromText("x") });
        Assert.Throws<ArgumentNullException>(() => renderer.Render(document, (Stream)null!));
    }

    [Fact]
    public void Render_With_NonWritable_Stream_Throws()
    {
        var renderer = new PdfRenderer();
        var document = new PdfDocument(new IDocumentElement[] { Paragraph.FromText("x") });
        using var readOnly = new MemoryStream(new byte[0], writable: false);
        Assert.Throws<ArgumentException>(() => renderer.Render(document, readOnly));
    }

    [Fact]
    public void Render_With_Empty_FilePath_Throws()
    {
        var renderer = new PdfRenderer();
        var document = new PdfDocument(new IDocumentElement[] { Paragraph.FromText("x") });
        Assert.Throws<ArgumentException>(() => renderer.Render(document, string.Empty));
    }

    [Fact]
    public async Task RenderAsync_To_File_Path_Writes_Valid_Pdf()
    {
        var document = new PdfDocument(new IDocumentElement[]
        {
            Heading.FromText(1, "AsyncFile"),
            Paragraph.FromText("Hello async file.")
        });

        var renderer = new PdfRenderer();
        var filePath = Path.Combine(Path.GetTempPath(), $"MarkdownToPdf-test-{Guid.NewGuid():N}.pdf");
        try
        {
            await renderer.RenderAsync(document, filePath);

            Assert.True(File.Exists(filePath));
            var content = Encoding.ASCII.GetString(await File.ReadAllBytesAsync(filePath));
            Assert.StartsWith("%PDF-1.4", content, StringComparison.Ordinal);
            Assert.Contains("(AsyncFile)", content, StringComparison.Ordinal);
        }
        finally
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
    }
}
