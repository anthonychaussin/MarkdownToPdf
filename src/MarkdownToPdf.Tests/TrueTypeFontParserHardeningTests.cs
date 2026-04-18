using System.Buffers.Binary;
using MarkdownToPdf.Fonts;

namespace MarkdownToPdf.Tests;

public sealed class TrueTypeFontParserHardeningTests
{
    [Fact]
    public void FromTrueTypeFile_TruncatedTableDirectory_ThrowsInvalidDataWithInvalidDataInner()
    {
        var bytes = new byte[12];
        WriteUInt16(bytes, 4, 1); // numTables

        var ex = AssertInvalidFont(bytes);
        Assert.IsType<InvalidDataException>(ex.InnerException);
    }

    [Fact]
    public void FromTrueTypeFile_CmapLengthOutOfRange_ThrowsInvalidDataWithInvalidDataInner()
    {
        var bytes = CreateMinimalFont(cmapSubtableFormat: 4, cmapSubtableLength: 5000);

        var ex = AssertInvalidFont(bytes);
        Assert.IsType<InvalidDataException>(ex.InnerException);
    }

    [Fact]
    public void FromTrueTypeFile_UnsupportedCmapFormat_ThrowsInvalidDataWithNotSupportedInner()
    {
        var bytes = CreateMinimalFont(cmapSubtableFormat: 12, cmapSubtableLength: 16);

        var ex = AssertInvalidFont(bytes);
        Assert.IsType<NotSupportedException>(ex.InnerException);
    }

    private static InvalidDataException AssertInvalidFont(byte[] bytes)
    {
        var path = Path.Combine(Path.GetTempPath(), $"MarkdownToPdf-parser-hardening-{Guid.NewGuid():N}.ttf");
        File.WriteAllBytes(path, bytes);

        try
        {
            return Assert.Throws<InvalidDataException>(() => PdfFont.FromTrueTypeFile(path));
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    private static byte[] CreateMinimalFont(ushort cmapSubtableFormat, ushort cmapSubtableLength)
    {
        const int tableDirectoryOffset = 12;
        const int tableRecordSize = 16;
        const int tableCount = 5;
        const int headOffset = 100;
        const int hheaOffset = 154;
        const int maxpOffset = 190;
        const int hmtxOffset = 196;
        const int cmapOffset = 200;

        var bytes = new byte[240];
        WriteUInt16(bytes, 4, tableCount);

        var recordBase = tableDirectoryOffset;
        WriteTableRecord(bytes, recordBase + (0 * tableRecordSize), "head", headOffset, 54);
        WriteTableRecord(bytes, recordBase + (1 * tableRecordSize), "hhea", hheaOffset, 36);
        WriteTableRecord(bytes, recordBase + (2 * tableRecordSize), "maxp", maxpOffset, 6);
        WriteTableRecord(bytes, recordBase + (3 * tableRecordSize), "hmtx", hmtxOffset, 4);
        WriteTableRecord(bytes, recordBase + (4 * tableRecordSize), "cmap", cmapOffset, 20);

        WriteUInt16(bytes, headOffset + 18, 1000); // unitsPerEm
        WriteInt16(bytes, headOffset + 36, 0); // xMin
        WriteInt16(bytes, headOffset + 38, 0); // yMin
        WriteInt16(bytes, headOffset + 40, 1000); // xMax
        WriteInt16(bytes, headOffset + 42, 1000); // yMax

        WriteInt16(bytes, hheaOffset + 4, 800); // ascent
        WriteInt16(bytes, hheaOffset + 6, -200); // descent
        WriteUInt16(bytes, hheaOffset + 34, 1); // numberOfHMetrics

        WriteUInt16(bytes, maxpOffset + 4, 1); // numGlyphs

        WriteUInt16(bytes, hmtxOffset, 500); // advance width

        WriteUInt16(bytes, cmapOffset, 0); // version
        WriteUInt16(bytes, cmapOffset + 2, 1); // numTables
        WriteUInt16(bytes, cmapOffset + 4, 3); // platform
        WriteUInt16(bytes, cmapOffset + 6, 1); // encoding
        WriteUInt32(bytes, cmapOffset + 8, 12); // subtable offset

        var subtableOffset = cmapOffset + 12;
        WriteUInt16(bytes, subtableOffset, cmapSubtableFormat);
        WriteUInt16(bytes, subtableOffset + 2, cmapSubtableLength);
        WriteUInt16(bytes, subtableOffset + 6, 2); // segCountX2 for format 4 paths

        return bytes;
    }

    private static void WriteTableRecord(byte[] bytes, int offset, string tag, uint tableOffset, uint length)
    {
        bytes[offset] = (byte)tag[0];
        bytes[offset + 1] = (byte)tag[1];
        bytes[offset + 2] = (byte)tag[2];
        bytes[offset + 3] = (byte)tag[3];
        WriteUInt32(bytes, offset + 8, tableOffset);
        WriteUInt32(bytes, offset + 12, length);
    }

    private static void WriteUInt16(byte[] bytes, int offset, ushort value)
    {
        BinaryPrimitives.WriteUInt16BigEndian(bytes.AsSpan(offset, 2), value);
    }

    private static void WriteUInt32(byte[] bytes, int offset, uint value)
    {
        BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(offset, 4), value);
    }

    private static void WriteInt16(byte[] bytes, int offset, short value)
    {
        BinaryPrimitives.WriteInt16BigEndian(bytes.AsSpan(offset, 2), value);
    }
}
