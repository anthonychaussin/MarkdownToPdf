using System.Globalization;
using MarkdownToPdf.Core.Document;

namespace MarkdownToPdf.Markdown;

internal static class MarkdownDirectiveParser
{
    public static bool IsPageBreak(string line)
    {
        var trimmed = line.Trim();
        return trimmed == "<!-- pagebreak -->"
               || trimmed == "<!-- page-break -->"
               || trimmed == "[[pagebreak]]"
               || trimmed == "\\pagebreak";
    }

    public static bool TryParseParagraphAlignment(string line, out ParagraphAlignment alignment)
    {
        alignment = ParagraphAlignment.Left;
        var trimmed = line.Trim();
        if (!trimmed.StartsWith("<!--", StringComparison.Ordinal) || !trimmed.EndsWith("-->", StringComparison.Ordinal))
        {
            return false;
        }

        var inner = trimmed[4..^3].Trim();
        if (!inner.StartsWith("align:", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var value = inner[6..].Trim();
        if (value.Equals("left", StringComparison.OrdinalIgnoreCase))
        {
            alignment = ParagraphAlignment.Left;
            return true;
        }

        if (value.Equals("center", StringComparison.OrdinalIgnoreCase))
        {
            alignment = ParagraphAlignment.Center;
            return true;
        }

        if (value.Equals("right", StringComparison.OrdinalIgnoreCase))
        {
            alignment = ParagraphAlignment.Right;
            return true;
        }

        return false;
    }

    public static bool TryParseTableDirective(
        string line,
        out TableBorderStyle style,
        out double? thickness,
        out double? cellPadding,
        out TableCellVerticalAlignment? verticalAlignment)
    {
        style = TableBorderStyle.Grid;
        thickness = null;
        cellPadding = null;
        verticalAlignment = null;
        var trimmed = line.Trim();
        if (!trimmed.StartsWith("<!--", StringComparison.Ordinal) || !trimmed.EndsWith("-->", StringComparison.Ordinal))
        {
            return false;
        }

        var inner = trimmed[4..^3].Trim();
        if (!inner.StartsWith("table:", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var value = inner[6..].Trim();
        if (value.Length == 0)
        {
            return false;
        }

        var normalized = value.Replace(';', ' ').Replace(',', ' ');
        var parts = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var found = false;

        foreach (var part in parts)
        {
            if (part.Equals("plain", StringComparison.OrdinalIgnoreCase) || part.Equals("none", StringComparison.OrdinalIgnoreCase))
            {
                style = TableBorderStyle.None;
                found = true;
                continue;
            }

            if (part.Equals("grid", StringComparison.OrdinalIgnoreCase))
            {
                style = TableBorderStyle.Grid;
                found = true;
                continue;
            }

            if (part.Equals("horizontal", StringComparison.OrdinalIgnoreCase))
            {
                style = TableBorderStyle.Horizontal;
                found = true;
                continue;
            }

            if (part.Equals("vertical", StringComparison.OrdinalIgnoreCase))
            {
                style = TableBorderStyle.Vertical;
                found = true;
                continue;
            }

            if (part.StartsWith("border=", StringComparison.OrdinalIgnoreCase))
            {
                var raw = part[7..].Trim();
                if (double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) && parsed > 0)
                {
                    thickness = parsed;
                    found = true;
                }
            }

            if (part.StartsWith("padding=", StringComparison.OrdinalIgnoreCase))
            {
                var raw = part[8..].Trim();
                if (double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) && parsed >= 0)
                {
                    cellPadding = parsed;
                    found = true;
                }
            }

            if (part.StartsWith("v=", StringComparison.OrdinalIgnoreCase))
            {
                var raw = part[2..].Trim();
                if (raw.Equals("top", StringComparison.OrdinalIgnoreCase))
                {
                    verticalAlignment = TableCellVerticalAlignment.Top;
                    found = true;
                }
                else if (raw.Equals("middle", StringComparison.OrdinalIgnoreCase) || raw.Equals("center", StringComparison.OrdinalIgnoreCase))
                {
                    verticalAlignment = TableCellVerticalAlignment.Middle;
                    found = true;
                }
                else if (raw.Equals("bottom", StringComparison.OrdinalIgnoreCase))
                {
                    verticalAlignment = TableCellVerticalAlignment.Bottom;
                    found = true;
                }
            }
        }

        return found;
    }

    public static bool TryParseQrCodeLine(string line, PdfTheme? theme, out QrCode qrCode)
    {
        qrCode = null!;
        var trimmed = line.Trim();
        if (!trimmed.StartsWith("QR[", StringComparison.OrdinalIgnoreCase) || !trimmed.EndsWith(']'))
        {
            return false;
        }

        var inner = trimmed[3..^1].Trim();
        if (inner.Length == 0)
        {
            return false;
        }

        var parts = inner.Split([';', ','], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0)
        {
            return false;
        }

        var content = parts[0];
        var ecc = QrErrorCorrectionLevel.M;
        double? size = null;

        for (var i = 1; i < parts.Length; i++)
        {
            var part = parts[i];
            if (part.StartsWith("ecc=", StringComparison.OrdinalIgnoreCase) ||
                part.StartsWith("level=", StringComparison.OrdinalIgnoreCase))
            {
                var value = part[(part.IndexOf('=') + 1)..].Trim();
                if (value.Length > 0)
                {
                    ecc = value[0] switch
                    {
                        'L' or 'l' => QrErrorCorrectionLevel.L,
                        'M' or 'm' => QrErrorCorrectionLevel.M,
                        'Q' or 'q' => QrErrorCorrectionLevel.Q,
                        'H' or 'h' => QrErrorCorrectionLevel.H,
                        _ => ecc
                    };
                }

                continue;
            }

            if (part.StartsWith("size=", StringComparison.OrdinalIgnoreCase))
            {
                var value = part[5..].Trim();
                if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) && parsed > 0)
                {
                    size = parsed;
                }
            }
        }

        var resolvedSize = size ?? (theme is null ? QrCode.DefaultSize : Math.Max(1, theme.BodyFontSize * 6));
        qrCode = new QrCode(content, resolvedSize, ecc);
        return true;
    }

    public static bool TryParseBarcodeLine(string line, out Barcode barcode)
    {
        barcode = null!;
        var trimmed = line.Trim();
        if (!trimmed.StartsWith("BAR[", StringComparison.OrdinalIgnoreCase) || !trimmed.EndsWith(']'))
        {
            return false;
        }

        var inner = trimmed[4..^1].Trim();
        if (inner.Length == 0)
        {
            return false;
        }

        var parts = inner.Split([';', ','], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0)
        {
            return false;
        }

        var content = parts[0];
        var kind = BarcodeKind.Code128;
        double? width = null;
        double? height = null;

        for (var i = 1; i < parts.Length; i++)
        {
            var part = parts[i];
            if (part.StartsWith("type=", StringComparison.OrdinalIgnoreCase))
            {
                var value = part[5..].Trim();
                if (value.Equals("code128", StringComparison.OrdinalIgnoreCase))
                {
                    kind = BarcodeKind.Code128;
                }

                continue;
            }

            if (part.StartsWith("width=", StringComparison.OrdinalIgnoreCase) || part.StartsWith("w=", StringComparison.OrdinalIgnoreCase))
            {
                var value = part[(part.IndexOf('=') + 1)..].Trim();
                if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) && parsed > 0)
                {
                    width = parsed;
                }

                continue;
            }

            if (part.StartsWith("height=", StringComparison.OrdinalIgnoreCase) || part.StartsWith("h=", StringComparison.OrdinalIgnoreCase))
            {
                var value = part[(part.IndexOf('=') + 1)..].Trim();
                if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) && parsed > 0)
                {
                    height = parsed;
                }
            }
        }

        var resolvedWidth = width ?? Barcode.DefaultWidth;
        var resolvedHeight = height ?? Barcode.DefaultHeight;
        barcode = new Barcode(content, kind, resolvedWidth, resolvedHeight);
        return true;
    }
}
