namespace MarkdownToPdf.Core.Document;

public sealed class Heading : IDocumentElement
{
    public int Level { get; }
    public IReadOnlyList<InlineRun> Inlines { get; }

    public Heading(int level, IReadOnlyList<InlineRun> inlines)
    {
        if (level < 1 || level > 6)
        {
            throw new ArgumentOutOfRangeException(nameof(level), "Heading level must be between 1 and 6.");
        }

        ArgumentNullException.ThrowIfNull(inlines);
        if (inlines.Count == 0)
        {
            throw new ArgumentException("Heading must contain at least one inline run.", nameof(inlines));
        }

        Inlines = [.. inlines];
        Level = level;
    }

    public static Heading FromText(int level, string text, TextStyle style = TextStyle.Regular)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException("Heading text must be non-empty.", nameof(text));
        }

        return new Heading(level, [new InlineRun(text, style)]);
    }
}
