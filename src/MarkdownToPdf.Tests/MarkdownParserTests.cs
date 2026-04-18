using MarkdownToPdf.Core.Document;
using MarkdownToPdf.Markdown;
using MarkdownToPdf.Markdown.Application.Abstractions;
using MarkdownToPdf.Core.Rendering;
using System.Text;

namespace MarkdownToPdf.Tests;

public sealed class MarkdownParserTests
{
    [Fact]
    public void Parser_Creates_Elements_From_Markdown()
    {
        var markdown = """
                       # Title
                       Paragraph with **bold** and *italic* and `code`.

                       - One
                       - Two

                       ---
                       """;

        var parser = new MarkdownDocumentParser();
        var document = parser.Parse(markdown);

        Assert.IsType<Heading>(document.Elements[0]);
        Assert.IsType<Paragraph>(document.Elements[1]);
        Assert.IsType<BulletList>(document.Elements[2]);
        Assert.IsType<HorizontalRule>(document.Elements[3]);
    }

    [Fact]
    public void Parser_Supports_Indented_List_Continuation_And_Backtick_Runs()
    {
        var markdown = """
                       - First line
                         continues here

                       Paragraph with ``inline code`` after.
                       """;

        var parser = new MarkdownDocumentParser();
        var document = parser.Parse(markdown);

        var list = Assert.IsType<BulletList>(document.Elements[0]);
        Assert.Single(list.Items);
        Assert.Contains("continues here", list.Items[0].Inlines[0].Text, StringComparison.Ordinal);
    }

    [Fact]
    public void Parser_Supports_Qr_And_Barcode_Lines()
    {
        var markdown = """
                       QR[https://example.com; ecc=H]
                       BAR[ABC-123]
                       """;

        var parser = new MarkdownDocumentParser();
        var document = parser.Parse(markdown);

        var qr = Assert.IsType<QrCode>(document.Elements[0]);
        Assert.Equal("https://example.com", qr.Content);
        Assert.Equal(QrErrorCorrectionLevel.H, qr.ErrorCorrection);

        var bar = Assert.IsType<Barcode>(document.Elements[1]);
        Assert.Equal("ABC-123", bar.Content);
        Assert.Equal(BarcodeKind.Code128, bar.Kind);
    }

    [Fact]
    public void RenderMarkdown_Uses_Injected_Parser()
    {
        var renderer = new PdfRenderer();
        var parser = new StubMarkdownParser();
        using var output = new MemoryStream();

        renderer.RenderMarkdown("ignored", output, parser);
        var content = Encoding.ASCII.GetString(output.ToArray());

        Assert.Contains("(From)", content, StringComparison.Ordinal);
        Assert.Contains("(stub)", content, StringComparison.Ordinal);
        Assert.Contains("(parser)", content, StringComparison.Ordinal);
    }

    private sealed class StubMarkdownParser : IMarkdownDocumentParser
    {
        public PdfDocument Parse(string markdown, PdfTheme? theme = null)
        {
            return new PdfDocument(
                [Paragraph.FromText("From stub parser")],
                theme);
        }

        public PdfDocument Parse(Stream markdownStream, PdfTheme? theme = null, Encoding? encoding = null)
        {
            return Parse(string.Empty, theme);
        }

        public PdfDocument ParseFile(string path, PdfTheme? theme = null, Encoding? encoding = null)
        {
            return Parse(string.Empty, theme);
        }
    }
}
