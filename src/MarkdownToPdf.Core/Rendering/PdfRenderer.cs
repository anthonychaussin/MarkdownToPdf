using MarkdownToPdf.Core.Application.Rendering;
using MarkdownToPdf.Core.Document;
using MarkdownToPdf.Core.Layout;

namespace MarkdownToPdf.Core.Rendering;

/// <summary>
/// High-level entry point for rendering <see cref="PdfDocument"/> instances to PDF.
/// The renderer is stateless and thread-safe; a single instance can be reused to
/// render many documents concurrently.
/// </summary>
/// <remarks>
/// All render paths write PDF data progressively to the supplied output <see cref="Stream"/>
/// (or file). No intermediate byte array containing the full document is ever
/// materialized, which keeps peak memory bounded regardless of document size.
/// Pooled buffers (<see cref="System.Buffers.ArrayPool{T}"/>) are used for transient
/// per-object payloads and are returned to the pool on both success and failure paths.
/// </remarks>
public sealed class PdfRenderer
{
    private readonly DocumentRenderPipeline _pipeline;

    /// <summary>
    /// Initializes a new <see cref="PdfRenderer"/> using the default layout engine
    /// and PDF writer.
    /// </summary>
    public PdfRenderer()
        : this(new DocumentRenderPipeline(
            new LayoutEngine(new BarcodeGenerator()),
            new PdfWriter()))
    {
    }

    internal PdfRenderer(DocumentRenderPipeline pipeline)
    {
        _pipeline = pipeline ?? throw new ArgumentNullException(nameof(pipeline));
    }

    /// <summary>
    /// Renders <paramref name="document"/> as a PDF directly to <paramref name="output"/>
    /// using default renderer options.
    /// </summary>
    /// <param name="document">The document to render. Must not be <see langword="null"/>.</param>
    /// <param name="output">A writable stream that will receive the PDF bytes. The
    /// caller owns the stream and is responsible for flushing/disposing it.</param>
    /// <exception cref="ArgumentNullException"><paramref name="document"/> or <paramref name="output"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="output"/> is not writable.</exception>
    public void Render(PdfDocument document, Stream output)
    {
        Render(document, output, options: null);
    }

    /// <summary>
    /// Renders <paramref name="document"/> as a PDF directly to <paramref name="output"/>.
    /// </summary>
    /// <param name="document">The document to render. Must not be <see langword="null"/>.</param>
    /// <param name="output">A writable stream that will receive the PDF bytes.</param>
    /// <param name="options">Optional renderer options (PDF/A conformance, output intent, ...).
    /// When <see langword="null"/>, defaults are used.</param>
    /// <exception cref="ArgumentNullException"><paramref name="document"/> or <paramref name="output"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="output"/> is not writable.</exception>
    public void Render(PdfDocument document, Stream output, PdfRendererOptions? options)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(output);
        EnsureWritableStream(output);

        _pipeline.Render(document, output, options);
    }

    /// <summary>
    /// Renders <paramref name="document"/> to the file located at <paramref name="filePath"/>.
    /// Any existing file at that location is overwritten.
    /// </summary>
    /// <param name="document">The document to render.</param>
    /// <param name="filePath">Destination file path. Must not be <see langword="null"/> or empty.</param>
    /// <exception cref="ArgumentNullException"><paramref name="document"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="filePath"/> is <see langword="null"/> or empty.</exception>
    public void Render(PdfDocument document, string filePath)
    {
        Render(document, filePath, options: null);
    }

    /// <summary>
    /// Renders <paramref name="document"/> to the file located at <paramref name="filePath"/>
    /// with the supplied renderer options.
    /// </summary>
    /// <param name="document">The document to render.</param>
    /// <param name="filePath">Destination file path.</param>
    /// <param name="options">Optional renderer options.</param>
    /// <exception cref="ArgumentNullException"><paramref name="document"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="filePath"/> is <see langword="null"/> or empty.</exception>
    public void Render(PdfDocument document, string filePath, PdfRendererOptions? options)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentException.ThrowIfNullOrEmpty(filePath);

        using var stream = CreateFileStream(filePath, useAsync: false);
        _pipeline.Render(document, stream, options);
    }

    /// <summary>
    /// Asynchronously renders <paramref name="document"/> to <paramref name="output"/>
    /// using default renderer options.
    /// </summary>
    /// <param name="document">The document to render.</param>
    /// <param name="output">A writable destination stream.</param>
    /// <param name="cancellationToken">Token used to observe cancellation.</param>
    /// <returns>A task that completes once the PDF has been fully written.</returns>
    public Task RenderAsync(PdfDocument document, Stream output, CancellationToken cancellationToken = default)
    {
        return RenderAsync(document, output, options: null, cancellationToken);
    }

    /// <summary>
    /// Asynchronously renders <paramref name="document"/> to <paramref name="output"/>.
    /// </summary>
    /// <param name="document">The document to render.</param>
    /// <param name="output">A writable destination stream.</param>
    /// <param name="options">Optional renderer options.</param>
    /// <param name="cancellationToken">Token used to observe cancellation.</param>
    /// <returns>A task that completes once the PDF has been fully written.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="document"/> or <paramref name="output"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="output"/> is not writable.</exception>
    public Task RenderAsync(
        PdfDocument document,
        Stream output,
        PdfRendererOptions? options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(output);
        EnsureWritableStream(output);
        cancellationToken.ThrowIfCancellationRequested();

        return Task.Run(
            () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                _pipeline.Render(document, output, options);
            },
            cancellationToken);
    }

    /// <summary>
    /// Asynchronously renders <paramref name="document"/> to the file located at
    /// <paramref name="filePath"/> using default renderer options.
    /// </summary>
    /// <param name="document">The document to render.</param>
    /// <param name="filePath">Destination file path.</param>
    /// <param name="cancellationToken">Token used to observe cancellation.</param>
    public Task RenderAsync(PdfDocument document, string filePath, CancellationToken cancellationToken = default)
    {
        return RenderAsync(document, filePath, options: null, cancellationToken);
    }

    /// <summary>
    /// Asynchronously renders <paramref name="document"/> to the file located at
    /// <paramref name="filePath"/>.
    /// </summary>
    /// <param name="document">The document to render.</param>
    /// <param name="filePath">Destination file path.</param>
    /// <param name="options">Optional renderer options.</param>
    /// <param name="cancellationToken">Token used to observe cancellation.</param>
    public Task RenderAsync(
        PdfDocument document,
        string filePath,
        PdfRendererOptions? options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentException.ThrowIfNullOrEmpty(filePath);
        cancellationToken.ThrowIfCancellationRequested();

        return Task.Run(
            () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                using var stream = CreateFileStream(filePath, useAsync: false);
                _pipeline.Render(document, stream, options);
            },
            cancellationToken);
    }

    private static FileStream CreateFileStream(string filePath, bool useAsync)
    {
        return new FileStream(
            filePath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 64 * 1024,
            useAsync: useAsync);
    }

    private static void EnsureWritableStream(Stream output)
    {
        if (!output.CanWrite)
        {
            throw new ArgumentException("The output stream must be writable.", nameof(output));
        }
    }
}
