using MarkdownToPdf.Core.Document;
using MarkdownToPdf.Core.Rendering;
using MarkdownToPdf.Markdown;
using System.Runtime.InteropServices;

var theme = ResolveTheme();

var markdown = """
# MarkdownToPdf Sample

This sample demonstrates a **markdown** document with *inline styles*, `code` spans,
multiple headings, lists with continuation, and code fences parsed into a PDF.

## Overview
MarkdownToPdf provides a small, focused API for building documents and rendering them to PDF.
The markdown parser is intentionally lightweight.
It supports headings, bullet lists, horizontal rules, paragraphs, and inline styles.

## Highlights
- Headings (levels 1-3)
  Continued list item text can be indented and will be folded into the same item.
- Bullet lists with multiple items
- Horizontal rules
- Inline styles: **bold**, *italic*, and `code`
- Code fences rendered in monospace
- QR codes and barcodes (Code128)

---

## Example Output Structure
The generated PDF will contain:
- A title and subtitles
- A few paragraphs
- A list with continuation lines
- A monospace block for commands

## Detailed Notes
### Inline parsing
Inline styles are detected in a simple pass:
`**bold**` uses bold font, `*italic*` uses italic font, and `` `code` `` uses monospace.

### Lists and paragraphs
Lists support a short, indented continuation line.
Paragraphs are assembled by merging consecutive non-empty lines.

## Usage
Run the sample, then open the generated PDF.

```
dotnet run --project samples/MarkdownToPdf.ConsoleSample/MarkdownToPdf.ConsoleSample.csproj
```

## Inline Styles
Mixing **bold**, *italic*, and `code` in a single paragraph works as expected.
Keep inline markup simple for predictable output.

## Code Fence
```
var theme = PdfTheme.Default();
var parser = new MarkdownDocumentParser();
var document = parser.Parse(markdown, theme);
```

## QR and Barcode
QR[https://MarkdownToPdf.test; ecc=H]
BAR[INV-2026-0007]

## Table (grid)
<!-- table: grid border=1 v=middle -->
| Feature | Status | Notes |
| :--- | ---: | :---: |
| Headings | OK | Levels 1-3 |
| Lists | OK | Basic bullets |
| Tables | OK | Grid enabled |

## Table (horizontal only)
<!-- table: horizontal border=0.6 v=top -->
| Column A | Column B |
| --- | --- |
| A1 | B1 |
| A2 | B2 |

## Table (vertical only)
<!-- table: vertical border=0.6 v=bottom -->
| Left | Right |
| --- | --- |
| L1 | R1 |
| L2 | R2 |

## Task List
- [ ] First task unchecked
- [x] Second task checked
- [ ] Third task unchecked

<!-- pagebreak -->

# Invoice Example

**Invoice No:** INV-2026-0007

**Date:** 2026-03-18

**Due Date:** 2026-04-17

#### Bill To
<!-- align: right -->
Acme Corp\\
123 Market Street\\
San Francisco, CA 94105\\

#### From
MarkdownToPdf Studio\\
456 Maple Avenue\\
Berlin, DE 10115\\

### Items
<!-- table: grid border=1 padding=6 v=middle -->
| Description | Qty | Unit Price | Line Total |
| :--- | ---: | ---: | ---: |
| Design & layout work | 6 | 120.00 | 720.00 |
| PDF rendering integration | 4 | 150.00 | 600.00 |
| Review & revisions | 2 | 90.00 | 180.00 |
| **Subtotal** |  |  | **1500.00** |
| **Tax (20%)** |  |  | **300.00** |
| **Total** |  |  | **1800.00** |

## Payment
Please pay within 30 days.  
IBAN: DE89 3704 0044 0532 0130 00  
BIC: COBADEFFXXX  

### Notes
> Thank you for your business.\\
> For questions, contact billing@MarkdownToPdf.test.
""";

var parser = new MarkdownDocumentParser();
var document = parser.Parse(markdown, theme);

var renderer = new PdfRenderer();
var outputPath = Path.Combine(AppContext.BaseDirectory, "sample.pdf");

using (var stream = File.Create(outputPath))
{
    renderer.Render(document, stream);
}

Console.WriteLine($"PDF generated at: {outputPath}");

static PdfTheme ResolveTheme()
{
    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
    {
        var windowsFonts = Environment.GetFolderPath(Environment.SpecialFolder.Fonts);
        var regular = Path.Combine(windowsFonts, "arial.ttf");
        var bold = Path.Combine(windowsFonts, "arialbd.ttf");
        var italic = Path.Combine(windowsFonts, "ariali.ttf");
        var monospace = Path.Combine(windowsFonts, "consola.ttf");

        if (File.Exists(regular) && File.Exists(bold) && File.Exists(italic) && File.Exists(monospace))
        {
            try
            {
                return PdfTheme.FromTrueTypeFiles(regular, bold, italic, monospace);
            }
            catch
            {
                // Fall back to standard fonts if parsing fails.
            }
        }
    }

    return PdfTheme.Default();
}
