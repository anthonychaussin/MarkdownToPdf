using System.Collections.Concurrent;
using MarkdownToPdf.Core.Document;
using MarkdownToPdf.Fonts;

namespace MarkdownToPdf.Core.Layout;

internal sealed class LayoutContext
{
    private readonly List<LayoutPage> _pages = [];
    private List<LayoutItem> _currentItems = [];
    private readonly ConcurrentDictionary<(TextStyle Style, double FontSize, string Text), double> _measureCache = new();
    private readonly ConcurrentDictionary<IDocumentElement, object> _layoutPlans = new(ReferenceEqualityComparer.Instance);

    public LayoutContext(PdfTheme theme)
    {
        Theme = theme ?? throw new ArgumentNullException(nameof(theme));
        Margins = theme.PageMargins;
        PageSize = theme.PageSize;
        X = Margins.Left;
        Y = PageSize.Height - Margins.Top;
        MaxX = PageSize.Width - Margins.Right;
    }

    public PdfTheme Theme { get; }
    public Margins Margins { get; }
    public PdfPageSize PageSize { get; }
    public double X { get; set; }
    public double Y { get; set; }
    public double MaxX { get; }
    public IList<LayoutItem> CurrentItems => _currentItems;

    public void CommitPage()
    {
        _pages.Add(new LayoutPage([.. _currentItems]));
        _currentItems = [];
        X = Margins.Left;
        Y = PageSize.Height - Margins.Top;
    }

    public void FinalizePendingPage()
    {
        if (_currentItems.Count > 0)
        {
            _pages.Add(new LayoutPage([.. _currentItems]));
        }
    }

    public LayoutDocument BuildDocument() => new([.. _pages]);

    public void EnsureLineSpace(double lineHeight)
    {
        if (Y - lineHeight < Margins.Bottom)
        {
            CommitPage();
        }
    }

    public void EnsureBlockSpace(double height)
    {
        if (Y - height < Margins.Bottom)
        {
            CommitPage();
        }
    }

    public double Measure(string text, TextStyle style, double fontSize)
    {
        using var _profileScope = LayoutProfiling.Scope(LayoutProfilePoint.Measure);
        var key = (style, fontSize, text);
        return _measureCache.GetOrAdd(
            key,
            static (k, state) =>
            {
                var font = ResolveFont(state, k.Style);
                return FontMetrics.MeasureText(font, k.FontSize, k.Text);
            },
            Theme);
    }

    public void SetPlan(IDocumentElement element, object plan)
    {
        ArgumentNullException.ThrowIfNull(element);
        ArgumentNullException.ThrowIfNull(plan);
        _layoutPlans[element] = plan;
    }

    public bool TryGetPlan<TPlan>(IDocumentElement element, out TPlan plan)
    {
        ArgumentNullException.ThrowIfNull(element);
        if (_layoutPlans.TryGetValue(element, out var value) && value is TPlan typed)
        {
            plan = typed;
            return true;
        }

        plan = default!;
        return false;
    }

    private static PdfFont ResolveFont(PdfTheme theme, TextStyle style)
    {
        return style switch
        {
            TextStyle.Regular => theme.RegularFont,
            TextStyle.Bold => theme.BoldFont,
            TextStyle.Italic => theme.ItalicFont,
            TextStyle.Monospace => theme.MonospaceFont,
            _ => throw new NotSupportedException($"Text style '{style}' is not supported.")
        };
    }
}
