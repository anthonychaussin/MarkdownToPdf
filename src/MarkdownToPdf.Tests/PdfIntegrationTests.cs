using System.Text;
using MarkdownToPdf.Core.Document;
using MarkdownToPdf.Core.Rendering;
using MarkdownToPdf.Fonts;

namespace MarkdownToPdf.Tests;

public sealed class PdfIntegrationTests
{
    [Fact]
    public void Render_To_File_Generates_NonEmpty_Pdf()
    {
        var fontPath = FindFontPath();
        var font = PdfFont.FromTrueTypeFile(fontPath);
        var theme = new PdfTheme(
            font,
            font,
            font,
            font,
            PdfPageSize.A4,
            Margins.Uniform(72));

        var document = new PdfDocument(new IDocumentElement[]
        {
            Heading.FromText(1, "Integration Test"),
            Paragraph.FromText("PDF output should exist and be non-empty.")
        }, theme);

        var renderer = new PdfRenderer();
        var outputPath = Path.Combine(Path.GetTempPath(), $"MarkdownToPdf-sample-{Guid.NewGuid():N}.pdf");

        try
        {
            using (var stream = File.Create(outputPath))
            {
                renderer.Render(document, stream);
            }

            var info = new FileInfo(outputPath);
            Assert.True(info.Exists);
            Assert.True(info.Length > 0);

            using var read = File.OpenRead(outputPath);
            var headerBytes = new byte[8];
            var readCount = read.Read(headerBytes, 0, headerBytes.Length);
            var header = Encoding.ASCII.GetString(headerBytes, 0, readCount);
            Assert.StartsWith("%PDF-1.4", header, StringComparison.Ordinal);
        }
        finally
        {
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }
        }
    }

    [Fact]
    public void Render_PdfA1B_Emits_Metadata_And_OutputIntent()
    {
        var fontPath = FindFontPath();
        var font = PdfFont.FromTrueTypeFile(fontPath);
        var theme = new PdfTheme(
            font,
            font,
            font,
            font,
            PdfPageSize.A4,
            Margins.Uniform(72));

        var document = new PdfDocument(new IDocumentElement[]
        {
            Heading.FromText(1, "PDF/A Test"),
            Paragraph.FromText("This document should carry PDF/A metadata.")
        }, theme);

        var renderer = new PdfRenderer();
        var options = new PdfRendererOptions
        {
            PdfAConformance = PdfAConformance.PdfA1B,
            PdfAOutputIntent = new PdfAOutputIntent(
                new byte[] { 1, 2, 3, 4 },
                "sRGB IEC61966-2.1",
                colorComponents: 3,
                info: "sRGB IEC61966-2.1")
        };

        using var stream = new MemoryStream();
        renderer.Render(document, stream, options);
        var content = Encoding.ASCII.GetString(stream.ToArray());

        Assert.Contains("/OutputIntents", content, StringComparison.Ordinal);
        Assert.Contains("/Metadata", content, StringComparison.Ordinal);
        Assert.Contains("GTS_PDFA1", content, StringComparison.Ordinal);
        Assert.Contains("pdfaid:conformance", content, StringComparison.Ordinal);
    }

    private static string FindFontPath()
    {
        var candidates = new List<string>();

        if (OperatingSystem.IsWindows())
        {
            var fontsDir = Environment.GetFolderPath(Environment.SpecialFolder.Fonts);
            if (!string.IsNullOrWhiteSpace(fontsDir))
            {
                candidates.Add(Path.Combine(fontsDir, "arial.ttf"));
                candidates.Add(Path.Combine(fontsDir, "segoeui.ttf"));
                candidates.Add(Path.Combine(fontsDir, "calibri.ttf"));
            }
        }
        else if (OperatingSystem.IsLinux())
        {
            candidates.Add("/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf");
            candidates.Add("/usr/share/fonts/truetype/liberation/LiberationSans-Regular.ttf");
        }
        else if (OperatingSystem.IsMacOS())
        {
            candidates.Add("/System/Library/Fonts/Supplemental/Arial.ttf");
            candidates.Add("/System/Library/Fonts/SFNS.ttf");
        }

        foreach (var path in candidates)
        {
            if (File.Exists(path))
            {
                return path;
            }
        }

        var fallbackDirs = new List<string>();
        if (OperatingSystem.IsWindows())
        {
            var fontsDir = Environment.GetFolderPath(Environment.SpecialFolder.Fonts);
            if (!string.IsNullOrWhiteSpace(fontsDir))
            {
                fallbackDirs.Add(fontsDir);
            }
        }
        else if (OperatingSystem.IsLinux())
        {
            fallbackDirs.Add("/usr/share/fonts");
            fallbackDirs.Add("/usr/local/share/fonts");
        }
        else if (OperatingSystem.IsMacOS())
        {
            fallbackDirs.Add("/System/Library/Fonts");
            fallbackDirs.Add("/Library/Fonts");
        }

        foreach (var dir in fallbackDirs)
        {
            if (!Directory.Exists(dir))
            {
                continue;
            }

            var match = Directory.EnumerateFiles(dir, "*.ttf", SearchOption.AllDirectories)
                .FirstOrDefault();
            if (match is not null)
            {
                return match;
            }
        }

        throw new FileNotFoundException("No TTF font file found on this machine to run the integration test.");
    }
}
