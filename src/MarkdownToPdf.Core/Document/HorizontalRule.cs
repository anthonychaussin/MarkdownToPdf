namespace MarkdownToPdf.Core.Document;

public sealed class HorizontalRule : IDocumentElement
{
    public double Thickness { get; }

    public HorizontalRule(double thickness = 1)
    {
        if (thickness <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(thickness), "Thickness must be positive.");
        }

        Thickness = thickness;
    }
}
