using System.Buffers;
using System.Globalization;
using System.IO.Compression;
using System.Text;
using MarkdownToPdf.Core.Application.Abstractions;
using MarkdownToPdf.Core.Document;
using MarkdownToPdf.Core.Layout;
using MarkdownToPdf.Fonts;

namespace MarkdownToPdf.Core.Rendering;

internal sealed partial class PdfWriter : IPdfDocumentWriter
{
    private const int CatalogId = 1;
    private const int PagesId = 2;
    private const int FirstFreeId = 3;

    private static readonly Dictionary<LayoutImage, ImageResource> EmptyImageResources = [];
    private static readonly byte[] PdfBinaryMarker = [0x25, 0xE2, 0xE3, 0xCF, 0xD3, 0x0A];
    private static readonly byte[] PdfHeaderBytes = "%PDF-1.4\n"u8.ToArray();

    public void Write(LayoutDocument document, PdfTheme theme, Stream output, PdfRendererOptions? options)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(theme);
        ArgumentNullException.ThrowIfNull(output);

        var pdfAConformance = options?.PdfAConformance;
        var pdfAOutputIntent = options?.PdfAOutputIntent;
        if (pdfAConformance is not null)
        {
            ValidatePdfA(theme, pdfAConformance.Value, pdfAOutputIntent);
        }

        var pageCount = document.Pages.Count;
        if (pageCount == 0)
        {
            throw new InvalidOperationException("Layout document must contain at least one page.");
        }

        var nextId = FirstFreeId;

        var (fontResources, fontWriteInfos) = AssignFontResourceIds(ref nextId, theme);

        int? metadataId = null;
        int? iccStreamId = null;
        int? outputIntentId = null;
        if (pdfAConformance is not null)
        {
            metadataId = nextId++;
            iccStreamId = nextId++;
            outputIntentId = nextId++;
        }

        var pageImageResources = AssignImageResourceIds(document, ref nextId, pageCount);

        var contentStreamIds = new int[pageCount];
        for (var i = 0; i < pageCount; i++)
        {
            contentStreamIds[i] = nextId++;
        }

        var pageObjectIds = new int[pageCount];
        for (var i = 0; i < pageCount; i++)
        {
            pageObjectIds[i] = nextId++;
        }

        var totalObjects = nextId - 1;
        var offsets = new long[totalObjects + 1];

        long position = 0;

        // Holds the pooled content-stream buffers produced by BuildContentStream until
        // they are consumed by WriteStreamObject. The outer try/finally guarantees the
        // rented arrays are returned to the pool even on exceptions (e.g. I/O errors
        // or a faulted parallel content-stream build).
        var contentStreamBuffers = new PooledBuffer[pageCount];
        try
        {
            output.Write(PdfHeaderBytes);
            position += PdfHeaderBytes.Length;
            output.Write(PdfBinaryMarker);
            position += PdfBinaryMarker.Length;

            foreach (var info in fontWriteInfos)
            {
                WriteFontObjects(output, ref position, offsets, info);
            }

            if (pdfAConformance is not null)
            {
                WritePdfAObjects(
                    output,
                    ref position,
                    offsets,
                    metadataId!.Value,
                    iccStreamId!.Value,
                    outputIntentId!.Value,
                    pdfAConformance.Value,
                    pdfAOutputIntent!);
            }

            for (var i = 0; i < pageCount; i++)
            {
                foreach (var kvp in pageImageResources[i])
                {
                    offsets[kvp.Value.ObjectId] = position;
                    WriteImageObject(output, ref position, kvp.Value.ObjectId, kvp.Key);
                }
            }

            if (pageCount > 1)
            {
                Parallel.For(0, pageCount, i =>
                {
                    contentStreamBuffers[i] = BuildContentStream(document.Pages[i], fontResources, pageImageResources[i]);
                });
            }
            else
            {
                contentStreamBuffers[0] = BuildContentStream(document.Pages[0], fontResources, pageImageResources[0]);
            }

            for (var i = 0; i < pageCount; i++)
            {
                var buffer = contentStreamBuffers[i];
                contentStreamBuffers[i] = default;
                try
                {
                    offsets[contentStreamIds[i]] = position;
                    WriteStreamObject(output, ref position, contentStreamIds[i], buffer.Buffer, buffer.Length, null);
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(buffer.Buffer);
                }
            }

            var distinctFontResources = ComputeDistinctFontResources(fontResources);

            var pageWidthText = theme.PageSize.Width.ToString("0.###", CultureInfo.InvariantCulture);
            var pageHeightText = theme.PageSize.Height.ToString("0.###", CultureInfo.InvariantCulture);

            for (var i = 0; i < pageCount; i++)
            {
                offsets[pageObjectIds[i]] = position;
                WritePageObject(
                    output,
                    ref position,
                    pageObjectIds[i],
                    contentStreamIds[i],
                    distinctFontResources,
                    pageImageResources[i],
                    pageWidthText,
                    pageHeightText);
            }

            offsets[PagesId] = position;
            WritePagesObject(output, ref position, pageObjectIds);

            offsets[CatalogId] = position;
            WriteCatalogObject(output, ref position, metadataId, outputIntentId);

            var xrefOffset = position;
            WriteXrefAndTrailer(output, ref position, offsets, totalObjects, xrefOffset);
        }
        finally
        {
            for (var i = 0; i < contentStreamBuffers.Length; i++)
            {
                var leftover = contentStreamBuffers[i].Buffer;
                if (leftover is not null)
                {
                    ArrayPool<byte>.Shared.Return(leftover);
                    contentStreamBuffers[i] = default;
                }
            }
        }
    }

    private static (Dictionary<TextStyle, FontResource> FontResources, FontWriteInfo[] WriteInfos) AssignFontResourceIds(
        ref int nextId,
        PdfTheme theme)
    {
        var infos = new FontWriteInfo[4];
        var resources = new Dictionary<TextStyle, FontResource>(4);
        var index = 0;

        AssignFont(ref nextId, theme.RegularFont, "F1", TextStyle.Regular, resources, infos, ref index);
        AssignFont(ref nextId, theme.BoldFont, "F2", TextStyle.Bold, resources, infos, ref index);
        AssignFont(ref nextId, theme.ItalicFont, "F3", TextStyle.Italic, resources, infos, ref index);
        AssignFont(ref nextId, theme.MonospaceFont, "F4", TextStyle.Monospace, resources, infos, ref index);

        return (resources, infos);
    }

    private static void AssignFont(
        ref int nextId,
        PdfFont font,
        string resourceName,
        TextStyle style,
        Dictionary<TextStyle, FontResource> resources,
        FontWriteInfo[] infos,
        ref int index)
    {
        if (font.Kind == PdfFontKind.Standard)
        {
            var id = nextId++;
            resources[style] = new FontResource(resourceName, id);
            infos[index++] = new FontWriteInfo(resourceName, font, null, null, id);
            return;
        }

        if (font.Kind != PdfFontKind.TrueType || font.TrueTypeData is null)
        {
            throw new NotSupportedException("Unsupported font kind.");
        }

        var fontFileId = nextId++;
        var descriptorId = nextId++;
        var objectId = nextId++;
        resources[style] = new FontResource(resourceName, objectId);
        infos[index++] = new FontWriteInfo(resourceName, font, fontFileId, descriptorId, objectId);
    }

    private static Dictionary<LayoutImage, ImageResource>[] AssignImageResourceIds(
        LayoutDocument document,
        ref int nextId,
        int pageCount)
    {
        var pageImageResources = new Dictionary<LayoutImage, ImageResource>[pageCount];
        for (var i = 0; i < pageCount; i++)
        {
            Dictionary<LayoutImage, ImageResource>? imageResources = null;
            var imageIndex = 1;
            foreach (var item in document.Pages[i].Items)
            {
                if (item is not LayoutImage image)
                {
                    continue;
                }

                imageResources ??= [];
                imageResources[image] = new ImageResource($"Im{imageIndex++}", nextId++);
            }

            pageImageResources[i] = imageResources ?? EmptyImageResources;
        }

        return pageImageResources;
    }

    private static List<FontResource> ComputeDistinctFontResources(Dictionary<TextStyle, FontResource> fontResources)
    {
        var distinct = new List<FontResource>(fontResources.Count);
        var seenNames = new HashSet<string>(StringComparer.Ordinal);
        foreach (var resource in fontResources.Values)
        {
            if (seenNames.Add(resource.ResourceName))
            {
                distinct.Add(resource);
            }
        }

        return distinct;
    }

    private static void ValidatePdfA(PdfTheme theme, PdfAConformance conformance, PdfAOutputIntent? outputIntent)
    {
        if (conformance != PdfAConformance.PdfA1B)
        {
            throw new NotSupportedException($"PDF/A conformance '{conformance}' is not supported.");
        }

        if (outputIntent is null)
        {
            throw new InvalidOperationException("PDF/A rendering requires an output intent with an ICC profile.");
        }

        EnsureEmbeddedFonts(theme);
    }

    private static void EnsureEmbeddedFonts(PdfTheme theme)
    {
        if (theme.RegularFont.Kind == PdfFontKind.Standard ||
            theme.BoldFont.Kind == PdfFontKind.Standard ||
            theme.ItalicFont.Kind == PdfFontKind.Standard ||
            theme.MonospaceFont.Kind == PdfFontKind.Standard)
        {
            throw new InvalidOperationException("PDF/A rendering requires embedded TrueType fonts.");
        }
    }

    private static void WriteFontObjects(Stream output, ref long position, long[] offsets, FontWriteInfo info)
    {
        if (info.Font.Kind == PdfFontKind.Standard)
        {
            offsets[info.FontObjectId] = position;
            using var writer = new PooledByteBuilder(96);
            writer.AppendAscii("<< /Type /Font /Subtype /Type1 /BaseFont /"u8);
            writer.AppendAscii(info.Font.Name);
            writer.AppendAscii(" >>"u8);
            WritePooledObject(output, ref position, info.FontObjectId, writer.Detach());
            return;
        }

        var data = info.Font.TrueTypeData!;

        offsets[info.FontFileId!.Value] = position;
        WriteStreamObject(output, ref position, info.FontFileId.Value, data.FontFile, data.FontFile.Length, null);

        offsets[info.FontDescriptorId!.Value] = position;
        {
            using var desc = new PooledByteBuilder(256);
            desc.AppendAscii("<< /Type /FontDescriptor "u8);
            desc.AppendAscii("/FontName /"u8);
            desc.AppendAscii(data.PostScriptName);
            desc.AppendAscii(" /Flags 32 /FontBBox ["u8);
            desc.AppendInt(data.XMin);
            desc.AppendByte((byte)' ');
            desc.AppendInt(data.YMin);
            desc.AppendByte((byte)' ');
            desc.AppendInt(data.XMax);
            desc.AppendByte((byte)' ');
            desc.AppendInt(data.YMax);
            desc.AppendAscii("] /ItalicAngle "u8);
            desc.AppendDouble(data.ItalicAngle);
            desc.AppendAscii(" /Ascent "u8);
            desc.AppendInt(data.Ascent);
            desc.AppendAscii(" /Descent "u8);
            desc.AppendInt(data.Descent);
            desc.AppendAscii(" /CapHeight "u8);
            desc.AppendInt(data.Ascent);
            desc.AppendAscii(" /StemV 80 /FontFile2 "u8);
            desc.AppendInt(info.FontFileId.Value);
            desc.AppendAscii(" 0 R >>"u8);
            WritePooledObject(output, ref position, info.FontDescriptorId.Value, desc.Detach());
        }

        offsets[info.FontObjectId] = position;
        {
            // Manual try/finally instead of `using var` because we need to pass
            // `obj` by ref to AppendWidths (a using-declared variable is readonly).
            var obj = new PooledByteBuilder(512);
            try
            {
                obj.AppendAscii("<< /Type /Font /Subtype /TrueType /BaseFont /"u8);
                obj.AppendAscii(data.PostScriptName);
                obj.AppendAscii(" /FirstChar 32 /LastChar 126 /Widths ["u8);
                AppendWidths(ref obj, data.Widths);
                obj.AppendAscii("] /FontDescriptor "u8);
                obj.AppendInt(info.FontDescriptorId.Value);
                obj.AppendAscii(" 0 R /Encoding /WinAnsiEncoding >>"u8);
                WritePooledObject(output, ref position, info.FontObjectId, obj.Detach());
            }
            finally
            {
                obj.Dispose();
            }
        }
    }

    private static void AppendWidths(scoped ref PooledByteBuilder writer, IReadOnlyDictionary<int, int> widths)
    {
        for (var code = 32; code <= 126; code++)
        {
            writer.AppendInt(widths.GetValueOrDefault(code));
            if (code < 126)
            {
                writer.AppendByte((byte)' ');
            }
        }
    }

    private static void WritePdfAObjects(
        Stream output,
        ref long position,
        long[] offsets,
        int metadataId,
        int iccStreamId,
        int outputIntentId,
        PdfAConformance conformance,
        PdfAOutputIntent pdfAOutputIntent)
    {
        var metadata = BuildPdfAXmp(conformance);
        offsets[metadataId] = position;
        WriteStreamObject(output, ref position, metadataId, metadata, metadata.Length, "/Type /Metadata /Subtype /XML");

        offsets[iccStreamId] = position;
        var iccExtra = $"/N {pdfAOutputIntent.ColorComponents}";
        WriteStreamObject(output, ref position, iccStreamId, pdfAOutputIntent.IccProfile, pdfAOutputIntent.IccProfile.Length, iccExtra);

        offsets[outputIntentId] = position;
        // Manual try/finally because `builder` is passed by ref to AppendEscapedText.
        var builder = new PooledByteBuilder(256);
        try
        {
            builder.AppendAscii("<< /Type /OutputIntent /S /GTS_PDFA1 /OutputConditionIdentifier ("u8);
            AppendEscapedText(ref builder, pdfAOutputIntent.OutputConditionIdentifier);
            builder.AppendAscii(") "u8);
            if (!string.IsNullOrWhiteSpace(pdfAOutputIntent.Info))
            {
                builder.AppendAscii("/Info ("u8);
                AppendEscapedText(ref builder, pdfAOutputIntent.Info);
                builder.AppendAscii(") "u8);
            }

            builder.AppendAscii("/DestOutputProfile "u8);
            builder.AppendInt(iccStreamId);
            builder.AppendAscii(" 0 R >>"u8);
            WritePooledObject(output, ref position, outputIntentId, builder.Detach());
        }
        finally
        {
            builder.Dispose();
        }
    }

    private static void WriteImageObject(Stream output, ref long position, int objectId, LayoutImage image)
    {
        var compressed = Compress(image.RgbData);
        var dict = new StringBuilder(128);
        dict.Append("/Type /XObject /Subtype /Image ");
        dict.Append("/Width ").Append(image.PixelWidth).Append(' ');
        dict.Append("/Height ").Append(image.PixelHeight).Append(' ');
        dict.Append("/ColorSpace /DeviceRGB /BitsPerComponent 8 ");
        dict.Append("/Filter /FlateDecode");

        WriteStreamObject(output, ref position, objectId, compressed, compressed.Length, dict.ToString());
    }

    private static void WritePageObject(
        Stream output,
        ref long position,
        int pageObjectId,
        int contentStreamId,
        IReadOnlyList<FontResource> distinctFontResources,
        IReadOnlyDictionary<LayoutImage, ImageResource> imageResources,
        string pageWidthText,
        string pageHeightText)
    {
        using var builder = new PooledByteBuilder(256);
        builder.AppendAscii("<< /Type /Page /Parent "u8);
        builder.AppendInt(PagesId);
        builder.AppendAscii(" 0 R /MediaBox [0 0 "u8);
        builder.AppendAscii(pageWidthText);
        builder.AppendByte((byte)' ');
        builder.AppendAscii(pageHeightText);
        builder.AppendAscii("] /Resources << /Font << "u8);

        for (var r = 0; r < distinctFontResources.Count; r++)
        {
            var resource = distinctFontResources[r];
            builder.AppendByte((byte)'/');
            builder.AppendAscii(resource.ResourceName);
            builder.AppendByte((byte)' ');
            builder.AppendInt(resource.FontObjectId);
            builder.AppendAscii(" 0 R "u8);
        }

        builder.AppendAscii(">> "u8);

        if (imageResources.Count > 0)
        {
            builder.AppendAscii("/XObject << "u8);
            foreach (var image in imageResources.Values)
            {
                builder.AppendByte((byte)'/');
                builder.AppendAscii(image.ResourceName);
                builder.AppendByte((byte)' ');
                builder.AppendInt(image.ObjectId);
                builder.AppendAscii(" 0 R "u8);
            }

            builder.AppendAscii(">> "u8);
        }

        builder.AppendAscii(">> /Contents "u8);
        builder.AppendInt(contentStreamId);
        builder.AppendAscii(" 0 R >>"u8);

        WritePooledObject(output, ref position, pageObjectId, builder.Detach());
    }

    private static void WritePagesObject(Stream output, ref long position, int[] pageObjectIds)
    {
        using var builder = new PooledByteBuilder(32 + pageObjectIds.Length * 10);
        builder.AppendAscii("<< /Type /Pages /Kids ["u8);
        for (var i = 0; i < pageObjectIds.Length; i++)
        {
            if (i > 0)
            {
                builder.AppendByte((byte)' ');
            }

            builder.AppendInt(pageObjectIds[i]);
            builder.AppendAscii(" 0 R"u8);
        }

        builder.AppendAscii("] /Count "u8);
        builder.AppendInt(pageObjectIds.Length);
        builder.AppendAscii(" >>"u8);
        WritePooledObject(output, ref position, PagesId, builder.Detach());
    }

    private static void WriteCatalogObject(Stream output, ref long position, int? metadataId, int? outputIntentId)
    {
        using var builder = new PooledByteBuilder(96);
        builder.AppendAscii("<< /Type /Catalog /Pages "u8);
        builder.AppendInt(PagesId);
        builder.AppendAscii(" 0 R "u8);
        if (metadataId.HasValue)
        {
            builder.AppendAscii("/Metadata "u8);
            builder.AppendInt(metadataId.Value);
            builder.AppendAscii(" 0 R "u8);
        }

        if (outputIntentId.HasValue)
        {
            builder.AppendAscii("/OutputIntents ["u8);
            builder.AppendInt(outputIntentId.Value);
            builder.AppendAscii(" 0 R] "u8);
        }

        builder.AppendAscii(">>"u8);
        WritePooledObject(output, ref position, CatalogId, builder.Detach());
    }

    private static void WriteXrefAndTrailer(
        Stream output,
        ref long position,
        long[] offsets,
        int totalObjects,
        long xrefOffset)
    {
        using var builder = new PooledByteBuilder(64 + totalObjects * 20);
        builder.AppendAscii("xref\n0 "u8);
        builder.AppendInt(totalObjects + 1);
        builder.AppendAscii("\n0000000000 65535 f \n"u8);

        Span<byte> offsetBuffer = stackalloc byte[10];
        for (var i = 1; i <= totalObjects; i++)
        {
            offsets[i].TryFormat(offsetBuffer, out _, "0000000000", CultureInfo.InvariantCulture);
            builder.AppendAscii(offsetBuffer);
            builder.AppendAscii(" 00000 n \n"u8);
        }

        builder.AppendAscii("trailer\n<< /Size "u8);
        builder.AppendInt(totalObjects + 1);
        builder.AppendAscii(" /Root "u8);
        builder.AppendInt(CatalogId);
        builder.AppendAscii(" 0 R >>\nstartxref\n"u8);
        builder.AppendInt(xrefOffset);
        builder.AppendAscii("\n%%EOF"u8);

        var detached = builder.Detach();
        try
        {
            output.Write(detached.Buffer, 0, detached.Length);
            position += detached.Length;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(detached.Buffer);
        }
    }

    private static void WritePooledObject(Stream output, ref long position, int id, PooledBuffer payload)
    {
        const int MaxPrefixLength = 24; // digits(id) + " 0 obj\n"
        const int SuffixLength = 8;     // "\nendobj\n"

        var combined = ArrayPool<byte>.Shared.Rent(MaxPrefixLength + payload.Length + SuffixLength);
        try
        {
            var span = combined.AsSpan();

            id.TryFormat(span, out var prefixLen, default, CultureInfo.InvariantCulture);
            span[prefixLen++] = (byte)' ';
            span[prefixLen++] = (byte)'0';
            span[prefixLen++] = (byte)' ';
            span[prefixLen++] = (byte)'o';
            span[prefixLen++] = (byte)'b';
            span[prefixLen++] = (byte)'j';
            span[prefixLen++] = (byte)'\n';

            Buffer.BlockCopy(payload.Buffer, 0, combined, prefixLen, payload.Length);
            var offset = prefixLen + payload.Length;

            "\nendobj\n"u8.CopyTo(span[offset..]);
            offset += SuffixLength;

            output.Write(combined, 0, offset);
            position += offset;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(combined);
            ArrayPool<byte>.Shared.Return(payload.Buffer);
        }
    }

    private static void WriteStreamObject(
        Stream output,
        ref long position,
        int id,
        byte[] payload,
        int payloadLength,
        string? additionalDictionary)
    {
        // Combines "id 0 obj\n<< /Length N ... >>\nstream\n" into a single buffer.
        using var header = new PooledByteBuilder(64);
        header.AppendInt(id);
        header.AppendAscii(" 0 obj\n<< /Length "u8);
        header.AppendInt(payloadLength);
        if (!string.IsNullOrWhiteSpace(additionalDictionary))
        {
            header.AppendByte((byte)' ');
            header.AppendAscii(additionalDictionary.Trim());
        }

        header.AppendAscii(" >>\nstream\n"u8);
        var headerBuffer = header.Detach();
        try
        {
            output.Write(headerBuffer.Buffer, 0, headerBuffer.Length);
            position += headerBuffer.Length;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(headerBuffer.Buffer);
        }

        output.Write(payload, 0, payloadLength);
        position += payloadLength;

        // Combines "\nendstream\nendobj\n" into a single write.
        output.Write("\nendstream\nendobj\n"u8);
        position += 18;
    }

    private static PooledBuffer BuildContentStream(
        LayoutPage page,
        IReadOnlyDictionary<TextStyle, FontResource> fontResources,
        IReadOnlyDictionary<LayoutImage, ImageResource> imageResources)
    {
        // Manual try/finally because `writer` is passed by ref to AppendEscapedText
        // (using-declared variables are readonly and can't be passed by ref).
        // The finally returns the rented buffer to the pool if any Append*
        // or unsupported-item path throws before we reach Detach().
        var writer = new PooledByteBuilder(page.Items.Count * 64);
        try
        {
            foreach (var item in page.Items)
            {
                switch (item)
                {
                    case LayoutText text:
                        writer.AppendAscii("BT /"u8);
                        writer.AppendAscii(fontResources[text.Style].ResourceName);
                        writer.AppendByte((byte)' ');
                        writer.AppendDouble(text.FontSize);
                        writer.AppendAscii(" Tf "u8);
                        writer.AppendDouble(text.X);
                        writer.AppendByte((byte)' ');
                        writer.AppendDouble(text.Y);
                        writer.AppendAscii(" Td ("u8);
                        AppendEscapedText(ref writer, text.Text);
                        writer.AppendAscii(") Tj ET\n"u8);
                        break;
                    case LayoutRule rule:
                        writer.AppendDouble(rule.Thickness);
                        writer.AppendAscii(" w "u8);
                        writer.AppendDouble(rule.X);
                        writer.AppendByte((byte)' ');
                        writer.AppendDouble(rule.Y);
                        writer.AppendAscii(" m "u8);
                        writer.AppendDouble(rule.X + rule.Width);
                        writer.AppendByte((byte)' ');
                        writer.AppendDouble(rule.Y);
                        writer.AppendAscii(" l S\n"u8);
                        break;
                    case LayoutLine line:
                        writer.AppendDouble(line.Thickness);
                        writer.AppendAscii(" w "u8);
                        writer.AppendDouble(line.X);
                        writer.AppendByte((byte)' ');
                        writer.AppendDouble(line.Y);
                        writer.AppendAscii(" m "u8);
                        writer.AppendDouble(line.X2);
                        writer.AppendByte((byte)' ');
                        writer.AppendDouble(line.Y2);
                        writer.AppendAscii(" l S\n"u8);
                        break;
                    case LayoutCheckbox checkbox:
                        var checkboxBottom = checkbox.Y - checkbox.Size;
                        writer.AppendDouble(checkbox.Thickness);
                        writer.AppendAscii(" w "u8);
                        writer.AppendDouble(checkbox.X);
                        writer.AppendByte((byte)' ');
                        writer.AppendDouble(checkboxBottom);
                        writer.AppendByte((byte)' ');
                        writer.AppendDouble(checkbox.Size);
                        writer.AppendByte((byte)' ');
                        writer.AppendDouble(checkbox.Size);
                        writer.AppendAscii(" re S\n"u8);

                        if (checkbox.IsChecked)
                        {
                            writer.AppendDouble(checkbox.Thickness);
                            writer.AppendAscii(" w "u8);
                            writer.AppendDouble(checkbox.X + checkbox.Size * 0.2);
                            writer.AppendByte((byte)' ');
                            writer.AppendDouble(checkboxBottom + checkbox.Size * 0.55);
                            writer.AppendAscii(" m "u8);
                            writer.AppendDouble(checkbox.X + checkbox.Size * 0.45);
                            writer.AppendByte((byte)' ');
                            writer.AppendDouble(checkboxBottom + checkbox.Size * 0.25);
                            writer.AppendAscii(" l "u8);
                            writer.AppendDouble(checkbox.X + checkbox.Size * 0.8);
                            writer.AppendByte((byte)' ');
                            writer.AppendDouble(checkboxBottom + checkbox.Size * 0.75);
                            writer.AppendAscii(" l S\n"u8);
                        }

                        break;
                    case LayoutImage image:
                        if (!imageResources.TryGetValue(image, out var resourceInfo))
                        {
                            throw new InvalidOperationException("Image resource was not registered for the page.");
                        }

                        writer.AppendAscii("q "u8);
                        writer.AppendDouble(image.Width);
                        writer.AppendAscii(" 0 0 "u8);
                        writer.AppendDouble(image.Height);
                        writer.AppendByte((byte)' ');
                        writer.AppendDouble(image.X);
                        writer.AppendByte((byte)' ');
                        writer.AppendDouble(image.Y - image.Height);
                        writer.AppendAscii(" cm /"u8);
                        writer.AppendAscii(resourceInfo.ResourceName);
                        writer.AppendAscii(" Do Q\n"u8);
                        break;
                    default:
                        throw new NotSupportedException($"Layout item '{item.GetType().Name}' is not supported.");
                }
            }

            return writer.Detach();
        }
        finally
        {
            writer.Dispose();
        }
    }

    private static void AppendEscapedText(scoped ref PooledByteBuilder writer, string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        for (var i = 0; i < text.Length; i++)
        {
            var ch = text[i];
            if (ch > 127)
            {
                throw new NotSupportedException("Only ASCII text is supported in the renderer for now.");
            }

            if (ch is '\\' or '(' or ')' or '\r' or '\n')
            {
                AppendEscapedTextSlow(ref writer, text, i);
                return;
            }
        }

        writer.AppendAscii(text);
    }

    private static void AppendEscapedTextSlow(scoped ref PooledByteBuilder writer, string text, int firstEscapeIndex)
    {
        if (firstEscapeIndex > 0)
        {
            writer.AppendAscii(text.AsSpan(0, firstEscapeIndex));
        }

        for (var i = firstEscapeIndex; i < text.Length; i++)
        {
            var ch = text[i];
            switch (ch)
            {
                case '\\':
                    writer.AppendByte((byte)'\\');
                    writer.AppendByte((byte)'\\');
                    break;
                case '(':
                    writer.AppendByte((byte)'\\');
                    writer.AppendByte((byte)'(');
                    break;
                case ')':
                    writer.AppendByte((byte)'\\');
                    writer.AppendByte((byte)')');
                    break;
                case '\r':
                case '\n':
                    writer.AppendByte((byte)' ');
                    break;
                default:
                    writer.AppendByte((byte)ch);
                    break;
            }
        }
    }

    private static byte[] Compress(byte[] data)
    {
        using var stream = new MemoryStream();
        using (var zlib = new ZLibStream(stream, CompressionLevel.Optimal, leaveOpen: true))
        {
            zlib.Write(data, 0, data.Length);
        }

        return stream.ToArray();
    }

    private static byte[] BuildPdfAXmp(PdfAConformance conformance)
    {
        var conformanceValue = conformance switch
        {
            PdfAConformance.PdfA1B => "B",
            _ => throw new NotSupportedException($"PDF/A conformance '{conformance}' is not supported.")
        };

        var builder = new StringBuilder();
        builder.Append("<?xpacket begin=\" \" id=\"W5M0MpCehiHzreSzNTczkc9d\"?>\n");
        builder.Append("<x:xmpmeta xmlns:x=\"adobe:ns:meta/\">\n");
        builder.Append(" <rdf:RDF xmlns:rdf=\"http://www.w3.org/1999/02/22-rdf-syntax-ns#\">\n");
        builder.Append("  <rdf:Description rdf:about=\"\" xmlns:pdfaid=\"http://www.aiim.org/pdfa/ns/id/\">\n");
        builder.Append("   <pdfaid:part>1</pdfaid:part>\n");
        builder.Append("   <pdfaid:conformance>").Append(conformanceValue).Append("</pdfaid:conformance>\n");
        builder.Append("  </rdf:Description>\n");
        builder.Append(" </rdf:RDF>\n");
        builder.Append("</x:xmpmeta>\n");
        builder.Append("<?xpacket end=\"w\"?>");

        return Encoding.UTF8.GetBytes(builder.ToString());
    }
}
