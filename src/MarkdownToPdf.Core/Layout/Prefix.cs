using MarkdownToPdf.Core.Document;

namespace MarkdownToPdf.Core.Layout;

internal sealed partial class TextLayoutComponent
{
    private readonly record struct Prefix(
        string? Text,
        TextStyle Style,
        bool IsCheckbox,
        bool IsChecked,
        double Size,
        double Gap,
        double Thickness)
    {
        public static Prefix ForText(string text, TextStyle style) => new(text, style, false, false, 0, 0, 0);
        public static Prefix ForCheckbox(bool isChecked, double size, double gap, double thickness) =>
            new(null, TextStyle.Regular, true, isChecked, size, gap, thickness);
    }
}
