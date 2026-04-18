namespace MarkdownToPdf.Core.Rendering;

internal sealed partial class PdfWriter
{
    private readonly record struct ImageResource(string ResourceName, int ObjectId);
}
