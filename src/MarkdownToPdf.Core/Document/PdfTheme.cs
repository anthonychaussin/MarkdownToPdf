using MarkdownToPdf.Fonts;

namespace MarkdownToPdf.Core.Document;

public sealed class PdfTheme(
    PdfFont regularFont,
    PdfFont boldFont,
    PdfFont italicFont,
    PdfFont monospaceFont,
    PdfPageSize pageSize,
    Margins pageMargins,
    double bodyFontSize = 12,
    double heading1Size = 24,
    double heading2Size = 18,
    double heading3Size = 14,
    double heading4Size = 13,
    double blockSpacing = 8)
{
    public PdfPageSize PageSize { get; } = pageSize;
    public Margins PageMargins { get; } = pageMargins;
    public PdfFont RegularFont { get; } = regularFont ?? throw new ArgumentNullException(nameof(regularFont));
    public PdfFont BoldFont { get; } = boldFont ?? throw new ArgumentNullException(nameof(boldFont));
    public PdfFont ItalicFont { get; } = italicFont ?? throw new ArgumentNullException(nameof(italicFont));
    public PdfFont MonospaceFont { get; } = monospaceFont ?? throw new ArgumentNullException(nameof(monospaceFont));
    public double BodyFontSize { get; } = RequirePositive(bodyFontSize, nameof(bodyFontSize));
    public double Heading1Size { get; } = RequirePositive(heading1Size, nameof(heading1Size));
    public double Heading2Size { get; } = RequirePositive(heading2Size, nameof(heading2Size));
    public double Heading3Size { get; } = RequirePositive(heading3Size, nameof(heading3Size));
    public double Heading4Size { get; } = RequirePositive(heading4Size, nameof(heading4Size));
    public double BlockSpacing { get; } = RequireNonNegative(blockSpacing, nameof(blockSpacing));

    public static PdfTheme Default()
    {
        return new PdfTheme(
            PdfFont.Standard(PdfStandardFont.Helvetica),
            PdfFont.Standard(PdfStandardFont.HelveticaBold),
            PdfFont.Standard(PdfStandardFont.HelveticaOblique),
            PdfFont.Standard(PdfStandardFont.Courier),
            PdfPageSize.A4,
            Margins.Uniform(72));
    }

    public static PdfTheme FromTrueTypeFile(
        string fontPath,
        PdfPageSize? pageSize = null,
        Margins? pageMargins = null,
        double bodyFontSize = 12,
        double heading1Size = 24,
        double heading2Size = 18,
        double heading3Size = 14,
        double heading4Size = 13,
        double blockSpacing = 8)
    {
        var font = PdfFont.FromTrueTypeFile(fontPath);
        return new PdfTheme(
            font,
            font,
            font,
            font,
            pageSize ?? PdfPageSize.A4,
            pageMargins ?? Margins.Uniform(72),
            bodyFontSize,
            heading1Size,
            heading2Size,
            heading3Size,
            heading4Size,
            blockSpacing);
    }

    public static PdfTheme FromTrueTypeFiles(
        string regularPath,
        string boldPath,
        string italicPath,
        string monospacePath,
        PdfPageSize? pageSize = null,
        Margins? pageMargins = null,
        double bodyFontSize = 12,
        double heading1Size = 24,
        double heading2Size = 18,
        double heading3Size = 14,
        double heading4Size = 13,
        double blockSpacing = 8)
    {
        return new PdfTheme(
            PdfFont.FromTrueTypeFile(regularPath),
            PdfFont.FromTrueTypeFile(boldPath),
            PdfFont.FromTrueTypeFile(italicPath),
            PdfFont.FromTrueTypeFile(monospacePath),
            pageSize ?? PdfPageSize.A4,
            pageMargins ?? Margins.Uniform(72),
            bodyFontSize,
            heading1Size,
            heading2Size,
            heading3Size,
            heading4Size,
            blockSpacing);
    }

    private static double RequirePositive(double value, string name)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(name, "Value must be positive.");
        }

        return value;
    }

    private static double RequireNonNegative(double value, string name)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(name, "Value must be non-negative.");
        }

        return value;
    }
}
