using MarkdownToPdf.Core.Application.Abstractions;
using MarkdownToPdf.Core.Document;

namespace MarkdownToPdf.Core.Layout;

internal sealed class LayoutEngine : IDocumentLayoutEngine
{
    private readonly IReadOnlyList<ILayoutComponent> _components;

    public LayoutEngine(IBarcodeImageGenerator barcodeImageGenerator)
    {
        ArgumentNullException.ThrowIfNull(barcodeImageGenerator);

        _components =
        [
            new StructureLayoutComponent(),
            new TextLayoutComponent(),
            new TableLayoutComponent(),
            new MediaLayoutComponent(barcodeImageGenerator)
        ];
    }

    public LayoutDocument Layout(PdfDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        var context = new LayoutContext(document.Theme);
        var componentsCount = _components.Count;
        foreach (var element in document.Elements)
        {
            if (element is not Table)
            {
                continue;
            }

            for (var i = 0; i < componentsCount; i++)
            {
                if (_components[i].TryPrepare(element, context))
                {
                    break;
                }
            }
        }

        foreach (var element in document.Elements)
        {
            var handled = false;
            for (var i = 0; i < componentsCount; i++)
            {
                if (_components[i].TryLayout(element, context))
                {
                    handled = true;
                    break;
                }
            }

            if (!handled)
            {
                throw new NotSupportedException($"Element type '{element.GetType().Name}' is not supported.");
            }
        }

        context.FinalizePendingPage();
        return context.BuildDocument();
    }
}
