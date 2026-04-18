using MarkdownToPdf.Fonts;

namespace MarkdownToPdf.Core.Rendering;

internal sealed partial class PdfWriter
{
    private readonly record struct FontWriteInfo(
        string ResourceName,
        PdfFont Font,
        int? FontFileId,
        int? FontDescriptorId,
        int FontObjectId);
}
