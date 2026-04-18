using MarkdownToPdf.Core.Document;

namespace MarkdownToPdf.Core.Layout;

internal sealed partial class TextLayoutComponent
{
    private sealed class TextLineState(double startX, int capacity)
    {
        public double StartX { get; set; } = startX;
        public bool HasContent { get; set; }
        public double Width { get; private set; }
        public List<(string Text, TextStyle Style, double Width, bool IsFragment)> Items { get; } = new List<(string Text, TextStyle Style, double Width, bool IsFragment)>(capacity);

        public void Add(string text, TextStyle style, double width, bool isFragment)
        {
            Items.Add((text, style, width, isFragment));
            Width += width;
            HasContent = true;
        }

        public void ResetForNextOutput()
        {
            Items.Clear();
            Width = 0;
            HasContent = false;
        }
    }
}
