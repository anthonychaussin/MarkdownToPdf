namespace MarkdownToPdf.Fonts;

public static class FontMetrics
{
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<PdfFont, int[]> AsciiWidthCache = new();

    public static double MeasureText(PdfFont font, double fontSize, string text)
    {
        ArgumentNullException.ThrowIfNull(font);
        ArgumentNullException.ThrowIfNull(text);

        if (fontSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(fontSize), "Font size must be positive.");
        }

        if (text.Length == 0)
        {
            return 0;
        }

        var widths = AsciiWidthCache.GetOrAdd(font, static f => BuildAsciiWidths(f));
        var totalUnits = 0;
        for (var i = 0; i < text.Length; i++)
        {
            var ch = text[i];
            if (ch > 127)
            {
                throw new NotSupportedException("Only ASCII text is supported in the renderer for now.");
            }

            var width = widths[ch];
            if (width < 0)
            {
                throw new NotSupportedException(
                    $"Font '{font.Name}' does not support character '{ch}' (U+{(int)ch:X4}).");
            }

            totalUnits += width;
        }

        return totalUnits * fontSize / 1000.0;
    }

    private static int[] BuildAsciiWidths(PdfFont font)
    {
        if (font.Kind == PdfFontKind.TrueType && font.TrueTypeData is not null)
        {
            var widths = new int[128];
            Array.Fill(widths, -1);
            for (var code = 0; code < widths.Length; code++)
            {
                if (font.TrueTypeData.Widths.TryGetValue(code, out var width))
                {
                    widths[code] = width;
                }
            }

            return widths;
        }

        var standardWidths = new int[128];
        for (var code = 0; code < standardWidths.Length; code++)
        {
            standardWidths[code] = code == ' ' ? 250 : 600;
        }

        return standardWidths;
    }
}
