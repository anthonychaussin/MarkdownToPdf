namespace MarkdownToPdf.Core.Rendering;

internal sealed partial class PdfWriter
{
    private readonly record struct PooledBuffer(byte[] Buffer, int Length);
}
