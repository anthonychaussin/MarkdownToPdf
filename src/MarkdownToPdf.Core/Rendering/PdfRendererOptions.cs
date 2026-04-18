namespace MarkdownToPdf.Core.Rendering;

/// <summary>
/// Options controlling how a <see cref="PdfRenderer"/> emits a PDF document.
/// </summary>
public sealed class PdfRendererOptions
{
    /// <summary>
    /// When set, requests that the renderer emits a PDF that conforms to the given
    /// PDF/A profile. Must be combined with a matching <see cref="PdfAOutputIntent"/>.
    /// </summary>
    public PdfAConformance? PdfAConformance { get; init; }

    /// <summary>
    /// ICC-based output intent to embed in the PDF when <see cref="PdfAConformance"/>
    /// is set. Required for PDF/A conformance.
    /// </summary>
    public PdfAOutputIntent? PdfAOutputIntent { get; init; }
}
