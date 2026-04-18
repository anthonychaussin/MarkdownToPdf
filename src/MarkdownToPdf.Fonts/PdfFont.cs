namespace MarkdownToPdf.Fonts;

public sealed class PdfFont
{
    public string Name { get; }
    public PdfFontKind Kind { get; }
    public TrueTypeFontData? TrueTypeData { get; }

    private PdfFont(string name, PdfFontKind kind, TrueTypeFontData? trueTypeData)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Kind = kind;
        TrueTypeData = trueTypeData;
    }

    public static PdfFont Standard(PdfStandardFont font)
    {
        var name = font switch
        {
            PdfStandardFont.Helvetica => "Helvetica",
            PdfStandardFont.HelveticaBold => "Helvetica-Bold",
            PdfStandardFont.HelveticaOblique => "Helvetica-Oblique",
            PdfStandardFont.Courier => "Courier",
            _ => throw new ArgumentOutOfRangeException(nameof(font), "Unknown standard font.")
        };

        return new PdfFont(name, PdfFontKind.Standard, null);
    }

    public static PdfFont FromTrueTypeFile(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Font path must be non-empty.", nameof(path));
        }

        if (!File.Exists(path))
        {
            throw new FileNotFoundException("Font file not found.", path);
        }

        byte[] bytes;
        try
        {
            bytes = File.ReadAllBytes(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            throw new IOException($"Unable to read TrueType font file '{path}'.", ex);
        }

        TrueTypeFontData data;
        try
        {
            data = TrueTypeFontParser.Parse(bytes);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            throw new InvalidDataException($"Invalid or unsupported TrueType font file '{path}'.", ex);
        }

        return new PdfFont(data.PostScriptName, PdfFontKind.TrueType, data);
    }
}
