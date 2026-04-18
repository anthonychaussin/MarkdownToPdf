namespace MarkdownToPdf.Core.Document;

public sealed class BlockQuote : IDocumentElement
{
    public IReadOnlyList<InlineRun> Inlines { get; }

    public BlockQuote(IReadOnlyList<InlineRun> inlines)
    {
        ArgumentNullException.ThrowIfNull(inlines);
        if (inlines.Count == 0)
        {
            throw new ArgumentException("Block quote must contain at least one inline run.", nameof(inlines));
        }

        Inlines = [.. inlines];
    }
}
