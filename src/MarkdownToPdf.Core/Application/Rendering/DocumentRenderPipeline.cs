using MarkdownToPdf.Core.Application.Abstractions;
using MarkdownToPdf.Core.Document;
using MarkdownToPdf.Core.Rendering;

namespace MarkdownToPdf.Core.Application.Rendering;

internal sealed class DocumentRenderPipeline(IDocumentLayoutEngine layoutEngine, IPdfDocumentWriter pdfWriter)
{
    private readonly IDocumentLayoutEngine _layoutEngine = layoutEngine ?? throw new ArgumentNullException(nameof(layoutEngine));
    private readonly IPdfDocumentWriter _pdfWriter = pdfWriter ?? throw new ArgumentNullException(nameof(pdfWriter));

    public void Render(PdfDocument document, Stream output, PdfRendererOptions? options)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(output);

        _pdfWriter.Write(_layoutEngine.Layout(document), document.Theme, output, options);
    }
}
