namespace MarkdownToPdf.Core.Document;

public sealed class QrCode : IDocumentElement
{
    public const double DefaultSize = 96;

    public string Content { get; }
    public double Size { get; }
    public QrErrorCorrectionLevel ErrorCorrection { get; }

    public QrCode(
        string content,
        double size = DefaultSize,
        QrErrorCorrectionLevel errorCorrection = QrErrorCorrectionLevel.M)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new ArgumentException("QR code content must be non-empty.", nameof(content));
        }

        if (size <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(size), "QR code size must be positive.");
        }

        Content = content;
        Size = size;
        ErrorCorrection = errorCorrection;
    }
}
