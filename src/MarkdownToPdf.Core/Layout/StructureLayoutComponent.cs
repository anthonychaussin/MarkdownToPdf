using MarkdownToPdf.Core.Document;

namespace MarkdownToPdf.Core.Layout;

internal sealed class StructureLayoutComponent : ILayoutComponent
{
    public bool TryLayout(IDocumentElement element, LayoutContext context)
    {
        switch (element)
        {
            case PageBreak:
                context.CommitPage();
                return true;
            case HorizontalRule rule:
                AddHorizontalRule(rule, context);
                return true;
            default:
                return false;
        }
    }

    private static void AddHorizontalRule(HorizontalRule rule, LayoutContext context)
    {
        var lineHeight = context.Theme.BodyFontSize * 1.2;
        var yLine = context.Y - (lineHeight * 0.2);
        var nextY = yLine - lineHeight - context.Theme.BlockSpacing;
        if (nextY < context.Margins.Bottom)
        {
            context.CommitPage();
            yLine = context.Y - (lineHeight * 0.2);
            nextY = yLine - lineHeight - context.Theme.BlockSpacing;
        }

        context.CurrentItems.Add(new LayoutRule(
            context.Margins.Left,
            yLine,
            context.PageSize.Width - context.Margins.Left - context.Margins.Right,
            rule.Thickness));
        context.Y = nextY;
    }
}
