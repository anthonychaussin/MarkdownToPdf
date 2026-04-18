namespace MarkdownToPdf.Core.Layout;

internal sealed class LayoutPage(IReadOnlyList<LayoutItem> items)
{
    public IReadOnlyList<LayoutItem> Items { get; } = items ?? throw new ArgumentNullException(nameof(items));
}
