using MarkdownToPdf.Core.Document;
using MarkdownToPdf.Core.Layout;
using MarkdownToPdf.Core.Rendering;

namespace MarkdownToPdf.Core.Application.Abstractions;

internal interface IPdfDocumentWriter
{
    void Write(LayoutDocument document, PdfTheme theme, Stream output, PdfRendererOptions? options);
}
