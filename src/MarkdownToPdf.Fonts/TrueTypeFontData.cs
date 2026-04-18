namespace MarkdownToPdf.Fonts;

public sealed class TrueTypeFontData(
    string postScriptName,
    int unitsPerEm,
    short ascent,
    short descent,
    short xMin,
    short yMin,
    short xMax,
    short yMax,
    double italicAngle,
    IReadOnlyDictionary<int, int> widths,
    byte[] fontFile)
{
    public string PostScriptName { get; } = postScriptName ?? throw new ArgumentNullException(nameof(postScriptName));
    public int UnitsPerEm { get; } = unitsPerEm;
    public short Ascent { get; } = ascent;
    public short Descent { get; } = descent;
    public short XMin { get; } = xMin;
    public short YMin { get; } = yMin;
    public short XMax { get; } = xMax;
    public short YMax { get; } = yMax;
    public double ItalicAngle { get; } = italicAngle;
    public IReadOnlyDictionary<int, int> Widths { get; } = widths ?? throw new ArgumentNullException(nameof(widths));
    public byte[] FontFile { get; } = fontFile ?? throw new ArgumentNullException(nameof(fontFile));
}
