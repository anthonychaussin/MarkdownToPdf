namespace MarkdownToPdf.Core.Document;

public sealed record InlineRun(string Text, TextStyle Style)
{
    public InlineRun(string text) : this(text, TextStyle.Regular)
    {
    }
}
