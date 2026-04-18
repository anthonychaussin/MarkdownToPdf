namespace MarkdownToPdf.Core.Rendering;

public sealed class PdfAOutputIntent
{
    public PdfAOutputIntent(byte[] iccProfile, string outputConditionIdentifier, int colorComponents = 3, string? info = null)
    {
        ArgumentNullException.ThrowIfNull(iccProfile);
        if (iccProfile.Length == 0)
        {
            throw new ArgumentException("ICC profile must not be empty.", nameof(iccProfile));
        }

        if (string.IsNullOrWhiteSpace(outputConditionIdentifier))
        {
            throw new ArgumentException("Output condition identifier is required.", nameof(outputConditionIdentifier));
        }

        if (colorComponents is < 1 or > 4)
        {
            throw new ArgumentOutOfRangeException(nameof(colorComponents), "Color components must be between 1 and 4.");
        }

        IccProfile = iccProfile;
        OutputConditionIdentifier = outputConditionIdentifier;
        Info = info;
        ColorComponents = colorComponents;
    }

    public byte[] IccProfile { get; }

    public string OutputConditionIdentifier { get; }

    public string? Info { get; }

    public int ColorComponents { get; }
}
