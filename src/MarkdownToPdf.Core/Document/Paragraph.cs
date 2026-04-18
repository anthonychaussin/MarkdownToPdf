namespace MarkdownToPdf.Core.Document;

public sealed class Paragraph : IDocumentElement
{
    public IReadOnlyList<InlineRun> Inlines { get; }
    public ParagraphAlignment Alignment { get; }

    public Paragraph(IReadOnlyList<InlineRun> inlines, ParagraphAlignment alignment = ParagraphAlignment.Left)
    {
        ArgumentNullException.ThrowIfNull(inlines);
        if (inlines.Count == 0)
        {
            throw new ArgumentException("Paragraph must contain at least one inline run.", nameof(inlines));
        }

        Inlines = [.. inlines];
        Alignment = alignment;
    }

    public static Paragraph FromText(
        string text,
        TextStyle style = TextStyle.Regular,
        ParagraphAlignment alignment = ParagraphAlignment.Left)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException("Paragraph text must be non-empty.", nameof(text));
        }

        return new Paragraph([new InlineRun(text, style)], alignment);
    }
}
