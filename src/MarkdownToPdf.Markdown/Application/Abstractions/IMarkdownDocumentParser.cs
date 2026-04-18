using System.Text;
using MarkdownToPdf.Core.Document;

namespace MarkdownToPdf.Markdown.Application.Abstractions;

public interface IMarkdownDocumentParser
{
    PdfDocument Parse(string markdown, PdfTheme? theme = null);

    PdfDocument Parse(Stream markdownStream, PdfTheme? theme = null, Encoding? encoding = null);

    PdfDocument ParseFile(string path, PdfTheme? theme = null, Encoding? encoding = null);
}
