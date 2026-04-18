namespace MarkdownToPdf.Core.Layout;

internal sealed class LayoutDocument(IReadOnlyList<LayoutPage> pages)
{
    public IReadOnlyList<LayoutPage> Pages { get; } = pages ?? throw new ArgumentNullException(nameof(pages));
}
