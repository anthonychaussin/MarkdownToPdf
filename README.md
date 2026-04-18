# MarkdownToPdf

[![NuGet](https://img.shields.io/nuget/v/MdToPdf.svg)](https://www.nuget.org/packages/MdToPdf)
[![NuGet Downloads](https://img.shields.io/nuget/dt/MdToPdf.svg)](https://www.nuget.org/packages/MdToPdf)
[![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4)](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
[![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)

A lightweight library for rendering structured documents (including Markdown) to PDF.

## NuGet

- Package page: [nuget.org/packages/MdToPdf](https://www.nuget.org/packages/MdToPdf)

Install with .NET CLI:

```bash
dotnet add package MdToPdf
```

| Assembly | Project in repo |
| --- | --- |
| `MarkdownToPdf.Core.dll` | `src/MarkdownToPdf.Core` |
| `MarkdownToPdf.Fonts.dll` | `src/MarkdownToPdf.Fonts` |
| `MarkdownToPdf.Markdown.dll` | `src/MarkdownToPdf.Markdown` |

Target frameworks: `net8.0` and `net10.0`.

## Quick start

```csharp
using MarkdownToPdf.Core.Document;
using MarkdownToPdf.Core.Rendering;
using MarkdownToPdf.Markdown;

var renderer = new PdfRenderer();

using (var file = File.Create("hello.pdf"))
{
    renderer.RenderMarkdown("# Hello\n\nFrom **MarkdownToPdf**.", file);
}

using var output = File.Create("doc.pdf");
renderer.Render(
    new PdfDocument(new IDocumentElement[]
    {
        Heading.FromText(1, "Title"),
        Paragraph.FromText("Body text."),
    }),
    output);
```

## License

MIT. See [`LICENSE`](LICENSE) (if present) or the `LICENSE` field in each package.
