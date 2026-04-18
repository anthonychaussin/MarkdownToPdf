using System.Text;
using MarkdownToPdf.Core.Document;
using MarkdownToPdf.Core.Rendering;
using MarkdownToPdf.Markdown.Application.Abstractions;
using MarkdownToPdf.Markdown.Application.Rendering;

namespace MarkdownToPdf.Markdown;

/// <summary>
/// <see cref="PdfRenderer"/> extension methods that parse Markdown content and
/// render it as PDF. All methods stream the PDF progressively to the destination;
/// no intermediate byte array is materialized.
/// </summary>
public static class PdfRendererExtensions
{
    private static readonly IMarkdownDocumentParser DefaultParser = new MarkdownDocumentParser();

    /// <summary>
    /// Parses the Markdown <paramref name="markdown"/> string and renders it to
    /// <paramref name="output"/> using the default Markdown parser.
    /// </summary>
    /// <param name="renderer">The PDF renderer to use.</param>
    /// <param name="markdown">Markdown source text.</param>
    /// <param name="output">Writable destination stream.</param>
    /// <param name="theme">Optional theme (fonts, page size, ...). When <see langword="null"/>,
    /// a default theme is used.</param>
    public static void RenderMarkdown(this PdfRenderer renderer, string markdown, Stream output, PdfTheme? theme = null)
    {
        RenderMarkdown(renderer, markdown, output, DefaultParser, theme);
    }

    /// <summary>
    /// Parses <paramref name="markdown"/> using the supplied <paramref name="markdownParser"/>
    /// and renders it to <paramref name="output"/>.
    /// </summary>
    /// <param name="renderer">The PDF renderer to use.</param>
    /// <param name="markdown">Markdown source text.</param>
    /// <param name="output">Writable destination stream.</param>
    /// <param name="markdownParser">Custom Markdown parser implementation.</param>
    /// <param name="theme">Optional theme.</param>
    public static void RenderMarkdown(
        this PdfRenderer renderer,
        string markdown,
        Stream output,
        IMarkdownDocumentParser markdownParser,
        PdfTheme? theme = null)
    {
        BuildPipeline(renderer, markdownParser).Render(markdown, output, theme);
    }

    /// <summary>
    /// Parses Markdown from <paramref name="markdownStream"/> using the default parser
    /// and renders it to <paramref name="output"/>.
    /// </summary>
    /// <param name="renderer">The PDF renderer to use.</param>
    /// <param name="markdownStream">Readable stream containing Markdown bytes.</param>
    /// <param name="output">Writable destination stream.</param>
    /// <param name="theme">Optional theme.</param>
    /// <param name="encoding">Text encoding used to decode the Markdown stream.
    /// Defaults to UTF-8.</param>
    public static void RenderMarkdown(this PdfRenderer renderer, Stream markdownStream, Stream output, PdfTheme? theme = null, Encoding? encoding = null)
    {
        RenderMarkdown(renderer, markdownStream, output, DefaultParser, theme, encoding);
    }

    /// <summary>
    /// Parses Markdown from <paramref name="markdownStream"/> using
    /// <paramref name="markdownParser"/> and renders it to <paramref name="output"/>.
    /// </summary>
    /// <param name="renderer">The PDF renderer to use.</param>
    /// <param name="markdownStream">Readable stream containing Markdown bytes.</param>
    /// <param name="output">Writable destination stream.</param>
    /// <param name="markdownParser">Custom Markdown parser implementation.</param>
    /// <param name="theme">Optional theme.</param>
    /// <param name="encoding">Text encoding. Defaults to UTF-8.</param>
    public static void RenderMarkdown(
        this PdfRenderer renderer,
        Stream markdownStream,
        Stream output,
        IMarkdownDocumentParser markdownParser,
        PdfTheme? theme = null,
        Encoding? encoding = null)
    {
        BuildPipeline(renderer, markdownParser).Render(markdownStream, output, theme, encoding);
    }

    /// <summary>
    /// Reads the Markdown file located at <paramref name="markdownPath"/> using
    /// the default parser and renders the result to <paramref name="output"/>.
    /// </summary>
    /// <param name="renderer">The PDF renderer to use.</param>
    /// <param name="markdownPath">Path to a Markdown source file.</param>
    /// <param name="output">Writable destination stream.</param>
    /// <param name="theme">Optional theme.</param>
    /// <param name="encoding">Text encoding. Defaults to UTF-8.</param>
    public static void RenderMarkdownFromFile(this PdfRenderer renderer, string markdownPath, Stream output, PdfTheme? theme = null, Encoding? encoding = null)
    {
        RenderMarkdownFromFile(renderer, markdownPath, output, DefaultParser, theme, encoding);
    }

    /// <summary>
    /// Reads the Markdown file located at <paramref name="markdownPath"/> using
    /// <paramref name="markdownParser"/> and renders the result to <paramref name="output"/>.
    /// </summary>
    /// <param name="renderer">The PDF renderer to use.</param>
    /// <param name="markdownPath">Path to a Markdown source file.</param>
    /// <param name="output">Writable destination stream.</param>
    /// <param name="markdownParser">Custom Markdown parser implementation.</param>
    /// <param name="theme">Optional theme.</param>
    /// <param name="encoding">Text encoding. Defaults to UTF-8.</param>
    public static void RenderMarkdownFromFile(
        this PdfRenderer renderer,
        string markdownPath,
        Stream output,
        IMarkdownDocumentParser markdownParser,
        PdfTheme? theme = null,
        Encoding? encoding = null)
    {
        BuildPipeline(renderer, markdownParser).RenderFromFile(markdownPath, output, theme, encoding);
    }

    /// <summary>
    /// Asynchronously parses and renders the Markdown <paramref name="markdown"/>
    /// string to <paramref name="output"/> using the default parser.
    /// </summary>
    /// <param name="renderer">The PDF renderer to use.</param>
    /// <param name="markdown">Markdown source text.</param>
    /// <param name="output">Writable destination stream.</param>
    /// <param name="theme">Optional theme.</param>
    /// <param name="cancellationToken">Token used to observe cancellation.</param>
    public static Task RenderMarkdownAsync(
        this PdfRenderer renderer,
        string markdown,
        Stream output,
        PdfTheme? theme = null,
        CancellationToken cancellationToken = default)
    {
        return RenderMarkdownAsync(renderer, markdown, output, DefaultParser, theme, cancellationToken);
    }

    /// <summary>
    /// Asynchronously parses <paramref name="markdown"/> with <paramref name="markdownParser"/>
    /// and renders it to <paramref name="output"/>.
    /// </summary>
    /// <param name="renderer">The PDF renderer to use.</param>
    /// <param name="markdown">Markdown source text.</param>
    /// <param name="output">Writable destination stream.</param>
    /// <param name="markdownParser">Custom Markdown parser implementation.</param>
    /// <param name="theme">Optional theme.</param>
    /// <param name="cancellationToken">Token used to observe cancellation.</param>
    public static Task RenderMarkdownAsync(
        this PdfRenderer renderer,
        string markdown,
        Stream output,
        IMarkdownDocumentParser markdownParser,
        PdfTheme? theme = null,
        CancellationToken cancellationToken = default)
    {
        var pipeline = BuildPipeline(renderer, markdownParser);
        return Task.Run(() => pipeline.Render(markdown, output, theme), cancellationToken);
    }

    private static MarkdownRenderPipeline BuildPipeline(PdfRenderer renderer, IMarkdownDocumentParser markdownParser)
    {
        ArgumentNullException.ThrowIfNull(renderer);
        ArgumentNullException.ThrowIfNull(markdownParser);
        return new MarkdownRenderPipeline(renderer, markdownParser);
    }
}
