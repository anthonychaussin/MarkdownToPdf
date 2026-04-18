namespace MarkdownToPdf.Core.Rendering;

internal sealed partial class PdfWriter
{
    private readonly record struct FontResource(string ResourceName, int FontObjectId);
}
