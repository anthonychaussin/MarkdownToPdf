using MarkdownToPdf.Core.Document;

namespace MarkdownToPdf.Tests;

public sealed class PdfDocumentTests
{
    [Fact]
    public void Document_Requires_AtLeastOneElement()
    {
        var ex = Assert.Throws<ArgumentException>(() => new PdfDocument(Array.Empty<IDocumentElement>()));
        Assert.Contains("at least one element", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Paragraph_Requires_Text()
    {
        var ex = Assert.Throws<ArgumentException>(() => Paragraph.FromText(" "));
        Assert.Contains("non-empty", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Heading_Level_OutOfRange_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Heading(0, new[] { new InlineRun("Title") }));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Heading(7, new[] { new InlineRun("Title") }));
    }
}
