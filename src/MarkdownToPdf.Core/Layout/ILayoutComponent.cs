using MarkdownToPdf.Core.Document;

namespace MarkdownToPdf.Core.Layout;

internal interface ILayoutComponent
{
    bool TryPrepare(IDocumentElement element, LayoutContext context) => false;

    bool TryLayout(IDocumentElement element, LayoutContext context);
}
