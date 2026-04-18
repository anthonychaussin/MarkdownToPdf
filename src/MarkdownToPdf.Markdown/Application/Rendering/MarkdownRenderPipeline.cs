using System.Text;
using MarkdownToPdf.Core.Document;
using MarkdownToPdf.Core.Rendering;
using MarkdownToPdf.Markdown.Application.Abstractions;

namespace MarkdownToPdf.Markdown.Application.Rendering;

internal sealed class MarkdownRenderPipeline(PdfRenderer renderer, IMarkdownDocumentParser markdownParser)
{
    private readonly PdfRenderer _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
    private readonly IMarkdownDocumentParser _markdownParser = markdownParser ?? throw new ArgumentNullException(nameof(markdownParser));

    public void Render(string markdown, Stream output, PdfTheme? theme)
    {
        _renderer.Render(_markdownParser.Parse(markdown, theme), output);
    }

    public void Render(Stream markdownStream, Stream output, PdfTheme? theme, Encoding? encoding)
    {
        _renderer.Render(_markdownParser.Parse(markdownStream, theme, encoding), output);
    }

    public void RenderFromFile(string markdownPath, Stream output, PdfTheme? theme, Encoding? encoding)
    {
        _renderer.Render(_markdownParser.ParseFile(markdownPath, theme, encoding), output);
    }
}
