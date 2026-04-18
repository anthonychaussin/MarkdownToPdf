using MarkdownToPdf.Core.Document;
using MarkdownToPdf.Core.Rendering;

namespace MarkdownToPdf.Core.Application.Abstractions;

internal interface IBarcodeImageGenerator
{
    ImageData GenerateQr(string content, int size, QrErrorCorrectionLevel errorCorrection);

    ImageData GenerateBarcode(string content, BarcodeKind kind, int width, int height);
}
