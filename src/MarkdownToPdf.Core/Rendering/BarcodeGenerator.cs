using ZXing;
using ZXing.Common;
using ZXing.OneD;
using ZXing.QrCode;
using ZXing.QrCode.Internal;
using MarkdownToPdf.Core.Application.Abstractions;
using MarkdownToPdf.Core.Document;

namespace MarkdownToPdf.Core.Rendering;

internal readonly record struct ImageData(int Width, int Height, byte[] RgbData);

internal sealed class BarcodeGenerator : IBarcodeImageGenerator
{
    public ImageData GenerateQr(string content, int size, QrErrorCorrectionLevel errorCorrection)
    {
        var hints = new Dictionary<EncodeHintType, object>
        {
            [EncodeHintType.ERROR_CORRECTION] = Map(errorCorrection),
            [EncodeHintType.MARGIN] = 0
        };

        var writer = new QRCodeWriter();
        var matrix = writer.encode(content, BarcodeFormat.QR_CODE, size, size, hints);
        return ToRgb(matrix);
    }

    public ImageData GenerateBarcode(string content, BarcodeKind kind, int width, int height)
    {
        var hints = new Dictionary<EncodeHintType, object>
        {
            [EncodeHintType.MARGIN] = 0
        };

        BitMatrix matrix = kind switch
        {
            BarcodeKind.Code128 => new Code128Writer().encode(content, BarcodeFormat.CODE_128, width, height, hints),
            _ => throw new NotSupportedException($"Barcode kind '{kind}' is not supported.")
        };

        return ToRgb(matrix);
    }

    private static ImageData ToRgb(BitMatrix matrix)
    {
        var width = matrix.Width;
        var height = matrix.Height;
        var rgb = new byte[width * height * 3];
        var destIndex = 0;

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var value = matrix[x, y] ? (byte)0 : (byte)255;
                rgb[destIndex++] = value;
                rgb[destIndex++] = value;
                rgb[destIndex++] = value;
            }
        }

        return new ImageData(width, height, rgb);
    }

    private static ErrorCorrectionLevel Map(QrErrorCorrectionLevel level)
    {
        return level switch
        {
            QrErrorCorrectionLevel.L => ErrorCorrectionLevel.L,
            QrErrorCorrectionLevel.M => ErrorCorrectionLevel.M,
            QrErrorCorrectionLevel.Q => ErrorCorrectionLevel.Q,
            QrErrorCorrectionLevel.H => ErrorCorrectionLevel.H,
            _ => ErrorCorrectionLevel.M
        };
    }
}
