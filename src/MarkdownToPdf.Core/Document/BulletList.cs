namespace MarkdownToPdf.Core.Document;

public sealed class BulletList : IDocumentElement
{
    public IReadOnlyList<ListItem> Items { get; }

    public BulletList(IReadOnlyList<ListItem> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        if (items.Count == 0)
        {
            throw new ArgumentException("Bullet list must contain at least one item.", nameof(items));
        }

        Items = [.. items];
    }
}
