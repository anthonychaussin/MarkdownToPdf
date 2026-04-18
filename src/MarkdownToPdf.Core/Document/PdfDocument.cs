namespace MarkdownToPdf.Core.Document;

/// <summary>
/// Immutable description of a PDF document: an ordered list of
/// <see cref="IDocumentElement"/> blocks and the theme used to render them.
/// </summary>
public sealed class PdfDocument
{
    /// <summary>
    /// The ordered block-level elements that make up the document (headings,
    /// paragraphs, tables, images, ...).
    /// </summary>
    public IReadOnlyList<IDocumentElement> Elements { get; }

    /// <summary>
    /// The theme (fonts, page size, margins, ...) applied when rendering.
    /// </summary>
    public PdfTheme Theme { get; }

    /// <summary>
    /// Creates a new <see cref="PdfDocument"/>.
    /// </summary>
    /// <param name="elements">Ordered block-level elements. Must contain at least one element.</param>
    /// <param name="theme">Optional theme. When <see langword="null"/>,
    /// <see cref="PdfTheme.Default"/> is used.</param>
    /// <exception cref="ArgumentNullException"><paramref name="elements"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="elements"/> is empty.</exception>
    public PdfDocument(IReadOnlyList<IDocumentElement> elements, PdfTheme? theme = null)
    {
        ArgumentNullException.ThrowIfNull(elements);
        if (elements.Count == 0)
        {
            throw new ArgumentException("Document must contain at least one element.", nameof(elements));
        }

        Elements = [.. elements];
        Theme = theme ?? PdfTheme.Default();
    }
}
