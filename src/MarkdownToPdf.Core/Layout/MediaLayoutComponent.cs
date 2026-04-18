using MarkdownToPdf.Core.Application.Abstractions;
using MarkdownToPdf.Core.Document;

namespace MarkdownToPdf.Core.Layout;

internal sealed class MediaLayoutComponent(IBarcodeImageGenerator barcodeImageGenerator) : ILayoutComponent
{
    private readonly IBarcodeImageGenerator _barcodeImageGenerator = barcodeImageGenerator ?? throw new ArgumentNullException(nameof(barcodeImageGenerator));

    public bool TryLayout(IDocumentElement element, LayoutContext context)
    {
        switch (element)
        {
            case QrCode qrCode:
                AddQrCode(qrCode, context);
                return true;
            case Barcode barcode:
                AddBarcode(barcode, context);
                return true;
            case PdfImage image:
                AddImage(image, context);
                return true;
            default:
                return false;
        }
    }

    private void AddQrCode(QrCode qrCode, LayoutContext context)
    {
        var size = Math.Min(qrCode.Size, context.MaxX - context.X);
        if (size <= 0)
        {
            return;
        }

        context.EnsureBlockSpace(size);

        var image = _barcodeImageGenerator.GenerateQr(qrCode.Content, Math.Max(1, (int)Math.Round(size)), qrCode.ErrorCorrection);
        context.CurrentItems.Add(new LayoutImage(context.X, context.Y, size, size, image.Width, image.Height, image.RgbData));
        context.Y -= size + context.Theme.BlockSpacing;
    }

    private void AddBarcode(Barcode barcode, LayoutContext context)
    {
        var width = Math.Min(barcode.Width, context.MaxX - context.X);
        var height = barcode.Height;
        if (width <= 0 || height <= 0)
        {
            return;
        }

        context.EnsureBlockSpace(height);

        var image = _barcodeImageGenerator.GenerateBarcode(
            barcode.Content,
            barcode.Kind,
            Math.Max(1, (int)Math.Round(width)),
            Math.Max(1, (int)Math.Round(height)));

        context.CurrentItems.Add(new LayoutImage(context.X, context.Y, width, height, image.Width, image.Height, image.RgbData));
        context.Y -= height + context.Theme.BlockSpacing;
    }

    private static void AddImage(PdfImage image, LayoutContext context)
    {
        var width = Math.Min(image.Width, context.MaxX - context.X);
        if (width <= 0)
        {
            return;
        }

        var scale = width / image.Width;
        var height = image.Height * scale;
        if (height <= 0)
        {
            return;
        }

        context.EnsureBlockSpace(height);
        context.CurrentItems.Add(new LayoutImage(context.X, context.Y, width, height, image.PixelWidth, image.PixelHeight, image.RgbData));
        context.Y -= height + context.Theme.BlockSpacing;
    }
}
