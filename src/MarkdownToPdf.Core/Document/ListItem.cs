namespace MarkdownToPdf.Core.Document;

public sealed class ListItem
{
    public IReadOnlyList<InlineRun> Inlines { get; }

    public ListItem(IReadOnlyList<InlineRun> inlines)
    {
        ArgumentNullException.ThrowIfNull(inlines);
        if (inlines.Count == 0)
        {
            throw new ArgumentException("List item must contain at least one inline run.", nameof(inlines));
        }

        Inlines = [.. inlines];
    }

    public static ListItem FromText(string text, TextStyle style = TextStyle.Regular)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException("List item text must be non-empty.", nameof(text));
        }

        return new ListItem([new InlineRun(text, style)]);
    }
}
