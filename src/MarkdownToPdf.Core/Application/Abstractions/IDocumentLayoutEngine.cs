using MarkdownToPdf.Core.Document;
using MarkdownToPdf.Core.Layout;

namespace MarkdownToPdf.Core.Application.Abstractions;

internal interface IDocumentLayoutEngine
{
    LayoutDocument Layout(PdfDocument document);
}
