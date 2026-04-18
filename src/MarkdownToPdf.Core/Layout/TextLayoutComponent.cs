using MarkdownToPdf.Core.Document;
using System.Text;

namespace MarkdownToPdf.Core.Layout;

internal sealed partial class TextLayoutComponent : ILayoutComponent
{
    private static readonly string[] AsciiSingleCharStrings =
    [
        ..Enumerable.Range(0, 128).Select(static i => ((char)i).ToString())
    ];

    public bool TryLayout(IDocumentElement element, LayoutContext context)
    {
        switch (element)
        {
            case Heading heading:
                AddHeadingBlock(heading, context);
                return true;
            case Paragraph paragraph:
                AddTextBlock(paragraph.Inlines, context.Theme.BodyFontSize, null, paragraph.Alignment, context);
                return true;
            case BulletList list:
                AddBulletList(list, context.Theme.BodyFontSize, context);
                return true;
            case TaskList taskList:
                AddTaskList(taskList, context.Theme.BodyFontSize, context);
                return true;
            case BlockQuote quote:
                AddBlockQuote(quote, context.Theme.BodyFontSize, context);
                return true;
            default:
                return false;
        }
    }

    private static void AddHeadingBlock(Heading heading, LayoutContext context)
    {
        var fontSize = GetHeadingSize(context.Theme, heading.Level);
        if (context.Y - context.Theme.BlockSpacing < context.Margins.Bottom)
        {
            context.CommitPage();
        }

        context.Y -= context.Theme.BlockSpacing;
        AddTextBlock(heading.Inlines, fontSize, null, ParagraphAlignment.Left, context, TextStyle.Bold);
    }

    private static void AddBulletList(BulletList list, double fontSize, LayoutContext context)
    {
        if (list.Items.Count == 0)
        {
            return;
        }

        foreach (var item in list.Items)
        {
            AddTextBlock(item.Inlines, fontSize, Prefix.ForText("- ", TextStyle.Regular), ParagraphAlignment.Left, context);
        }
    }

    private static void AddTaskList(TaskList list, double fontSize, LayoutContext context)
    {
        if (list.Items.Count == 0)
        {
            return;
        }

        var boxSize = fontSize * 0.75;
        var gap = fontSize * 0.35;
        var thickness = Math.Max(0.6, fontSize * 0.06);
        foreach (var item in list.Items)
        {
            AddTextBlock(item.Inlines, fontSize, Prefix.ForCheckbox(item.IsChecked, boxSize, gap, thickness), ParagraphAlignment.Left, context);
        }
    }

    private static void AddBlockQuote(BlockQuote quote, double fontSize, LayoutContext context)
    {
        var indent = fontSize * 1.4;
        var savedX = context.X;
        var startY = context.Y;

        context.X = savedX + indent;

        AddTextBlock(quote.Inlines, fontSize, null, ParagraphAlignment.Left, context, TextStyle.Italic);

        var endY = context.Y;
        context.X = savedX;

        var lineX = savedX + indent * 0.4;
        context.CurrentItems.Add(new LayoutLine(savedX + indent * 0.4, startY + fontSize, lineX, endY + fontSize, Math.Max(0.6, fontSize * 0.06)));
    }

    private static void AddTextBlock(
        IReadOnlyList<InlineRun> inlines,
        double fontSize,
        Prefix? prefix,
        ParagraphAlignment alignment,
        LayoutContext context,
        TextStyle? nonMonospaceStyleOverride = null)
    {
        using var _profileScope = LayoutProfiling.Scope(LayoutProfilePoint.AddTextBlock);
        var paragraph = new ParagraphLayoutState(fontSize, alignment);
        var line = new TextLineState(context.X, Math.Max(inlines.Count * 2, 16));
        var atLogicalLineStart = true;

        context.EnsureLineSpace(paragraph.LineHeight);

        if (prefix.HasValue)
        {
            var prefixValue = prefix.Value;
            if (prefixValue.IsCheckbox)
            {
                var boxTop = context.Y + (paragraph.FontSize * 1.5) - prefixValue.Size;
                context.CurrentItems.Add(new LayoutCheckbox(
                    line.StartX,
                    boxTop,
                    prefixValue.Size,
                    prefixValue.Thickness,
                    prefixValue.IsChecked));
                line.StartX += prefixValue.Size + prefixValue.Gap;
            }
            else
            {
                var prefixWidth = context.Measure(prefixValue.Text!, prefixValue.Style, paragraph.FontSize);
                context.CurrentItems.Add(new LayoutText(prefixValue.Text!, line.StartX, context.Y, paragraph.FontSize, prefixValue.Style));
                line.StartX += prefixWidth;
            }

            line.HasContent = true;
        }

        void FlushLine()
        {
            if (line.Items.Count == 0)
            {
                return;
            }

            var startX = paragraph.Alignment == ParagraphAlignment.Left
                ? line.StartX
                : CalculateAlignedStartX(paragraph.Alignment, line.StartX, context.MaxX, line.Width);
            var cursor = startX;
            string? pendingText = null;
            TextStyle pendingStyle = default;
            var pendingX = 0.0;
            var pendingIsFragment = false;
            StringBuilder? pendingBuilder = null;

            void EmitPending()
            {
                if (pendingText is null)
                {
                    return;
                }

                context.CurrentItems.Add(new LayoutText(
                    pendingBuilder is null ? pendingText! : pendingBuilder.ToString(),
                    pendingX,
                    context.Y,
                    paragraph.FontSize,
                    pendingStyle));
                pendingText = null;
                pendingBuilder = null;
                pendingIsFragment = false;
            }

            var itemCount = line.Items.Count;
            for (var i = 0; i < itemCount; i++)
            {
                var (Text, Style, Width, IsFragment) = line.Items[i];
                if (pendingText is null)
                {
                    pendingText = Text;
                    pendingStyle = Style;
                    pendingX = cursor;
                    pendingIsFragment = IsFragment;
                    cursor += Width;
                    continue;
                }

                if (Style == pendingStyle && pendingIsFragment && IsFragment)
                {
                    if (pendingBuilder is null)
                    {
                        pendingBuilder = new StringBuilder(pendingText.Length + Text.Length);
                        pendingBuilder.Append(pendingText);
                    }

                    pendingBuilder.Append(Text);
                    cursor += Width;
                    continue;
                }

                EmitPending();
                pendingText = Text;
                pendingStyle = Style;
                pendingX = cursor;
                pendingIsFragment = IsFragment;
                cursor += Width;
            }

            EmitPending();
            line.ResetForNextOutput();
        }

        void AddToken(string tokenText, TextStyle tokenStyle, bool isSpace)
        {
            if (isSpace && !line.HasContent)
            {
                return;
            }

            var width = context.Measure(tokenText, tokenStyle, paragraph.FontSize);
            var overflows = line.StartX + line.Width + width > context.MaxX;
            if (overflows && line.HasContent)
            {
                FlushLine();
                context.Y -= paragraph.LineHeight;
                context.EnsureLineSpace(paragraph.LineHeight);
            }

            if (overflows && !line.HasContent && !isSpace)
            {
                var tokenLength = tokenText.Length;
                for (var i = 0; i < tokenLength; i++)
                {
                    var charText = CharToStringCached(tokenText[i]);
                    var charWidth = context.Measure(charText, tokenStyle, paragraph.FontSize);
                    if (line.StartX + line.Width + charWidth > context.MaxX && line.HasContent)
                    {
                        FlushLine();
                        context.Y -= paragraph.LineHeight;
                        context.EnsureLineSpace(paragraph.LineHeight);
                    }

                    line.Add(charText, tokenStyle, charWidth, isFragment: true);
                }

                return;
            }

            line.Add(tokenText, tokenStyle, width, isFragment: false);
        }

        for (var inlineIndex = 0; inlineIndex < inlines.Count; inlineIndex++)
        {
            var style = ApplyStyleOverride(inlines[inlineIndex].Style, nonMonospaceStyleOverride);
            var text = inlines[inlineIndex].Text;
            var segmentStart = -1;
            var textLength = text.Length;

            for (var charIndex = 0; charIndex < textLength; charIndex++)
            {
                var ch = text[charIndex];
                if (ch is '\r' or '\n')
                {
                    if (segmentStart >= 0)
                    {
                        AddToken(text[segmentStart..charIndex], style, isSpace: false);
                        segmentStart = -1;
                    }

                    FlushLine();
                    context.Y -= paragraph.LineHeight;
                    context.EnsureLineSpace(paragraph.LineHeight);
                    atLogicalLineStart = true;
                    continue;
                }

                if (ch == ' ')
                {
                    if (segmentStart >= 0)
                    {
                        AddToken(text[segmentStart..charIndex], style, isSpace: false);
                        segmentStart = -1;
                    }

                    if (!atLogicalLineStart)
                    {
                        AddToken(" ", style, isSpace: true);
                    }

                    atLogicalLineStart = line.Items.Count == 0 && !line.HasContent;
                    continue;
                }

                if (segmentStart < 0)
                {
                    segmentStart = charIndex;
                }

                atLogicalLineStart = false;
            }

            if (segmentStart >= 0)
            {
                AddToken(text[segmentStart..], style, isSpace: false);
            }
        }

        FlushLine();
        context.Y -= paragraph.LineHeight + context.Theme.BlockSpacing;
    }

    internal static IReadOnlyList<IReadOnlyList<InlineRun>> WrapInlineRuns(
        IReadOnlyList<InlineRun> inlines,
        double fontSize,
        double maxWidth,
        TextStyle? styleOverride,
        LayoutContext context)
    {
        var wrapped = WrapInlineRunsWithWidths(inlines, fontSize, maxWidth, styleOverride, context);
        var lines = new List<IReadOnlyList<InlineRun>>(wrapped.Count);
        for (var i = 0; i < wrapped.Count; i++)
        {
            lines.Add(wrapped[i].Inlines);
        }

        return lines;
    }

    internal static IReadOnlyList<WrappedInlineLine> WrapInlineRunsWithWidths(
        IReadOnlyList<InlineRun> inlines,
        double fontSize,
        double maxWidth,
        TextStyle? styleOverride,
        LayoutContext context)
    {
        using var _profileScope = LayoutProfiling.Scope(LayoutProfilePoint.WrapInlineRuns);
        if (maxWidth <= 0)
        {
            return [new WrappedInlineLine(inlines, MeasureRuns(inlines, fontSize, styleOverride, context))];
        }

        var lines = new List<WrappedInlineLine>();
        var current = new List<InlineRun>(16);
        var currentWidth = 0.0;
        var atLogicalLineStart = true;

        void FlushLine()
        {
            if (current.Count == 0)
            {
                return;
            }

            lines.Add(new WrappedInlineLine(current, currentWidth));
            current = [];
            currentWidth = 0.0;
        }

        void AddToken(string tokenText, TextStyle style, bool isSpace)
        {
            if (isSpace && current.Count == 0)
            {
                return;
            }

            var tokenWidth = context.Measure(tokenText, style, fontSize);
            if (currentWidth + tokenWidth <= maxWidth)
            {
                current.Add(new InlineRun(tokenText, style));
                currentWidth += tokenWidth;
                return;
            }

            if (isSpace)
            {
                FlushLine();
                return;
            }

            if (current.Count > 0)
            {
                FlushLine();
            }

            if (tokenWidth <= maxWidth)
            {
                current.Add(new InlineRun(tokenText, style));
                currentWidth += tokenWidth;
                return;
            }

            for (var i = 0; i < tokenText.Length; i++)
            {
                var chText = CharToStringCached(tokenText[i]);
                var chWidth = context.Measure(chText, style, fontSize);
                if (currentWidth + chWidth > maxWidth && current.Count > 0)
                {
                    FlushLine();
                }

                current.Add(new InlineRun(chText, style));
                currentWidth += chWidth;
            }
        }

        var inlinesCount = inlines.Count;
        for (var inlineIndex = 0; inlineIndex < inlinesCount; inlineIndex++)
        {
            var style = styleOverride ?? inlines[inlineIndex].Style;
            var text = inlines[inlineIndex].Text;
            var segmentStart = -1;
            var textLength = text.Length;

            for (var charIndex = 0; charIndex < textLength; charIndex++)
            {
                var ch = text[charIndex];
                if (ch is '\r' or '\n')
                {
                    if (segmentStart >= 0)
                    {
                        AddToken(text[segmentStart..charIndex], style, isSpace: false);
                        segmentStart = -1;
                    }

                    FlushLine();
                    atLogicalLineStart = true;
                    continue;
                }

                if (ch == ' ')
                {
                    if (segmentStart >= 0)
                    {
                        AddToken(text[segmentStart..charIndex], style, isSpace: false);
                        segmentStart = -1;
                    }

                    if (!atLogicalLineStart)
                    {
                        AddToken(" ", style, isSpace: true);
                    }

                    atLogicalLineStart = current.Count == 0;
                    continue;
                }

                if (segmentStart < 0)
                {
                    segmentStart = charIndex;
                }

                atLogicalLineStart = false;
            }

            if (segmentStart >= 0)
            {
                AddToken(text[segmentStart..], style, isSpace: false);
            }
        }

        FlushLine();
        return lines;
    }

    private static double MeasureRuns(
        IReadOnlyList<InlineRun> runs,
        double fontSize,
        TextStyle? styleOverride,
        LayoutContext context)
    {
        var total = 0.0;
        for (var i = 0; i < runs.Count; i++)
        {
            total += context.Measure(runs[i].Text, styleOverride ?? runs[i].Style, fontSize);
        }

        return total;
    }

    private static string CharToStringCached(char ch) =>
        ch < AsciiSingleCharStrings.Length ? AsciiSingleCharStrings[ch] : ch.ToString();

    private static TextStyle ApplyStyleOverride(TextStyle original, TextStyle? overrideStyle)
    {
        if (overrideStyle is null || original == TextStyle.Monospace)
        {
            return original;
        }

        return overrideStyle.Value;
    }

    private static double GetHeadingSize(PdfTheme theme, int level) =>
        level switch
        {
            1 => theme.Heading1Size,
            2 => theme.Heading2Size,
            3 => theme.Heading3Size,
            4 => theme.Heading4Size,
            5 => theme.Heading4Size,
            6 => theme.Heading4Size,
            _ => throw new ArgumentOutOfRangeException(nameof(level), "Heading level must be between 1 and 6.")
        };

    private static double CalculateAlignedStartX(
        ParagraphAlignment alignment,
        double lineStartX,
        double maxX,
        double lineWidth)
    {
        return alignment switch
        {
            ParagraphAlignment.Right => Math.Max(lineStartX, maxX - lineWidth),
            ParagraphAlignment.Center => lineStartX + Math.Max(0, ((maxX - lineStartX) - lineWidth) / 2),
            _ => lineStartX
        };
    }
}
