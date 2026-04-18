using System.Text;
using MarkdownToPdf.Core.Document;
using MarkdownToPdf.Core.Rendering;
using MarkdownToPdf.Markdown;
using MarkdownToPdf.Markdown.Application.Abstractions;

namespace MarkdownToPdf.Tests;

public sealed class PdfRendererMarkdownExtensionsTests
{
    [Fact]
    public void RenderMarkdown_WithNullRenderer_ThrowsArgumentNullException()
    {
        PdfRenderer renderer = null!;
        using var output = new MemoryStream();

        Assert.Throws<ArgumentNullException>(() => renderer.RenderMarkdown("# title", output));
    }

    [Fact]
    public void RenderMarkdown_WithNullParser_ThrowsArgumentNullException()
    {
        var renderer = new PdfRenderer();
        using var output = new MemoryStream();

        Assert.Throws<ArgumentNullException>(() => renderer.RenderMarkdown("# title", output, (IMarkdownDocumentParser)null!));
    }

    [Fact]
    public void RenderMarkdown_StreamOverload_WithNullMarkdownStream_ThrowsArgumentNullException()
    {
        var renderer = new PdfRenderer();
        using var output = new MemoryStream();

        Assert.Throws<ArgumentNullException>(() => renderer.RenderMarkdown((Stream)null!, output));
    }

    [Fact]
    public void RenderMarkdown_StreamOverload_WithNullOutput_ThrowsArgumentNullException()
    {
        var renderer = new PdfRenderer();
        using var input = new MemoryStream(Encoding.UTF8.GetBytes("# title"));

        Assert.Throws<ArgumentNullException>(() => renderer.RenderMarkdown(input, null!));
    }

    [Fact]
    public void RenderMarkdownFromFile_WithEmptyPath_ThrowsArgumentException()
    {
        var renderer = new PdfRenderer();
        using var output = new MemoryStream();

        Assert.Throws<ArgumentException>(() => renderer.RenderMarkdownFromFile(" ", output));
    }

    [Fact]
    public void RenderMarkdownFromFile_WithMissingFile_ThrowsFileNotFoundException()
    {
        var renderer = new PdfRenderer();
        using var output = new MemoryStream();
        var missingPath = Path.Combine(Path.GetTempPath(), $"missing-{Guid.NewGuid():N}.md");

        Assert.Throws<FileNotFoundException>(() => renderer.RenderMarkdownFromFile(missingPath, output));
    }

    [Fact]
    public void RenderMarkdown_FromStream_LeavesInputStreamOpen()
    {
        var renderer = new PdfRenderer();
        using var input = new MemoryStream(Encoding.UTF8.GetBytes("# title"));
        using var output = new MemoryStream();

        renderer.RenderMarkdown(input, output);

        Assert.True(input.CanRead);
    }

    [Fact]
    public void RenderMarkdown_FromStream_ConsumesInputToEnd()
    {
        var renderer = new PdfRenderer();
        using var input = new MemoryStream(Encoding.UTF8.GetBytes("# title"));
        using var output = new MemoryStream();

        renderer.RenderMarkdown(input, output);

        Assert.Equal(input.Length, input.Position);
    }

    [Fact]
    public void RenderMarkdown_WithValidMarkdown_ProducesPdfHeader()
    {
        var renderer = new PdfRenderer();
        using var output = new MemoryStream();

        renderer.RenderMarkdown("# title", output);
        var bytes = output.ToArray();
        var header = Encoding.ASCII.GetString(bytes, 0, 5);

        Assert.Equal("%PDF-", header);
    }

    [Fact]
    public void RenderMarkdown_StreamOverload_UsesInjectedParser()
    {
        var renderer = new PdfRenderer();
        var parser = new TrackingParser();
        using var input = new MemoryStream(Encoding.UTF8.GetBytes("ignored"));
        using var output = new MemoryStream();

        renderer.RenderMarkdown(input, output, parser);
        var content = Encoding.ASCII.GetString(output.ToArray());

        Assert.Equal(1, parser.ParseStreamCalls);
        Assert.Equal(0, parser.ParseStringCalls);
        Assert.Equal(0, parser.ParseFileCalls);
        Assert.Contains("(from)", content, StringComparison.Ordinal);
        Assert.Contains("(stream)", content, StringComparison.Ordinal);
    }

    [Fact]
    public void RenderMarkdown_FileOverload_UsesInjectedParser()
    {
        var renderer = new PdfRenderer();
        var parser = new TrackingParser();
        var markdownPath = Path.GetTempFileName();

        try
        {
            File.WriteAllText(markdownPath, "ignored");

            using var output = new MemoryStream();
            renderer.RenderMarkdownFromFile(markdownPath, output, parser);
            var content = Encoding.ASCII.GetString(output.ToArray());

            Assert.Equal(0, parser.ParseStringCalls);
            Assert.Equal(0, parser.ParseStreamCalls);
            Assert.Equal(1, parser.ParseFileCalls);
            Assert.Contains("(from)", content, StringComparison.Ordinal);
            Assert.Contains("(file)", content, StringComparison.Ordinal);
        }
        finally
        {
            File.Delete(markdownPath);
        }
    }

    [Fact]
    public async Task RenderMarkdownAsync_UsesInjectedParser()
    {
        var renderer = new PdfRenderer();
        var parser = new TrackingParser();
        using var output = new MemoryStream();

        await renderer.RenderMarkdownAsync("ignored", output, parser);
        var content = Encoding.ASCII.GetString(output.ToArray());

        Assert.Equal(1, parser.ParseStringCalls);
        Assert.Equal(0, parser.ParseStreamCalls);
        Assert.Equal(0, parser.ParseFileCalls);
        Assert.Contains("(from)", content, StringComparison.Ordinal);
        Assert.Contains("(string)", content, StringComparison.Ordinal);
    }

    private sealed class TrackingParser : IMarkdownDocumentParser
    {
        public int ParseStringCalls { get; private set; }

        public int ParseStreamCalls { get; private set; }

        public int ParseFileCalls { get; private set; }

        public PdfDocument Parse(string markdown, PdfTheme? theme = null)
        {
            ParseStringCalls++;
            return CreateDocument("from string");
        }

        public PdfDocument Parse(Stream markdownStream, PdfTheme? theme = null, Encoding? encoding = null)
        {
            ParseStreamCalls++;
            return CreateDocument("from stream");
        }

        public PdfDocument ParseFile(string path, PdfTheme? theme = null, Encoding? encoding = null)
        {
            ParseFileCalls++;
            return CreateDocument("from file");
        }

        private static PdfDocument CreateDocument(string text)
        {
            return new PdfDocument([Paragraph.FromText(text)]);
        }
    }
}
