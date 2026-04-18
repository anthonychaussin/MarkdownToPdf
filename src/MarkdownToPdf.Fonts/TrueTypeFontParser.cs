using System.Buffers.Binary;
using System.Text;

namespace MarkdownToPdf.Fonts;

internal static class TrueTypeFontParser
{
    private const int OffsetTableSize = 12;
    private const int TableRecordSize = 16;

    public static TrueTypeFontData Parse(byte[] data)
    {
        if (data is null || data.Length == 0)
        {
            throw new ArgumentException("Font data must be non-empty.", nameof(data));
        }

        if (data.Length < OffsetTableSize)
        {
            throw new InvalidDataException("Invalid TrueType font: missing offset table.");
        }

        var reader = new BigEndianReader(data);
        reader.Skip(4);
        var tableCount = reader.ReadUInt16();
        reader.Skip(6);

        EnsureRange(data, OffsetTableSize, checked((uint)tableCount * TableRecordSize), "table directory");

        var tables = new Dictionary<string, TableRecord>(StringComparer.Ordinal);
        for (var i = 0; i < tableCount; i++)
        {
            var tag = reader.ReadTag();
            _ = reader.ReadUInt32();
            var offset = reader.ReadUInt32();
            var length = reader.ReadUInt32();
            EnsureRange(data, offset, length, $"table '{tag}'");
            tables[tag] = new TableRecord(offset, length);
        }

        var head = RequireTable(tables, "head");
        var hhea = RequireTable(tables, "hhea");
        var maxp = RequireTable(tables, "maxp");
        var hmtx = RequireTable(tables, "hmtx");
        var cmap = RequireTable(tables, "cmap");
        TableRecord? post = tables.TryGetValue("post", out var postTable) ? postTable : null;
        TableRecord? name = tables.TryGetValue("name", out var nameTable) ? nameTable : null;

        EnsureMinimumLength(head, 54, "head");
        EnsureMinimumLength(hhea, 36, "hhea");
        EnsureMinimumLength(maxp, 6, "maxp");
        EnsureMinimumLength(cmap, 4, "cmap");

        var unitsPerEm = ReadUInt16At(data, head.Offset + 18);
        if (unitsPerEm == 0)
        {
            throw new InvalidDataException("Invalid TrueType font: unitsPerEm is zero.");
        }

        var xMin = ReadInt16At(data, head.Offset + 36);
        var yMin = ReadInt16At(data, head.Offset + 38);
        var xMax = ReadInt16At(data, head.Offset + 40);
        var yMax = ReadInt16At(data, head.Offset + 42);

        var ascent = ReadInt16At(data, hhea.Offset + 4);
        var descent = ReadInt16At(data, hhea.Offset + 6);
        var numberOfHMetrics = ReadUInt16At(data, hhea.Offset + 34);

        var numGlyphs = ReadUInt16At(data, maxp.Offset + 4);
        var advanceWidths = ReadAdvanceWidths(data, hmtx, numberOfHMetrics, numGlyphs);

        var cmapMap = ReadCmap(data, cmap);
        var widths = BuildWidths(cmapMap, advanceWidths, unitsPerEm);

        var italicAngle = 0.0;
        if (post.HasValue)
        {
            EnsureMinimumLength(post.Value, 8, "post");
            italicAngle = ReadFixedAt(data, post.Value.Offset + 4);
        }

        var postScriptName = ReadPostScriptName(data, name);

        return new TrueTypeFontData(
            postScriptName,
            unitsPerEm,
            ascent,
            descent,
            xMin,
            yMin,
            xMax,
            yMax,
            italicAngle,
            widths,
            data);
    }

    private static Dictionary<int, int> BuildWidths(
        Dictionary<int, int> cmap,
        List<ushort> advanceWidths,
        int unitsPerEm)
    {
        var widths = new Dictionary<int, int>();
        if (advanceWidths.Count == 0)
        {
            return widths;
        }

        for (var code = 32; code <= 126; code++)
        {
            if (!cmap.TryGetValue(code, out var glyphIndex))
            {
                continue;
            }

            var width = glyphIndex < advanceWidths.Count
                ? advanceWidths[glyphIndex]
                : advanceWidths[^1];

            widths[code] = (int)Math.Round(width * 1000.0 / unitsPerEm);
        }

        return widths;
    }

    private static List<ushort> ReadAdvanceWidths(byte[] data, TableRecord hmtxTable, ushort numberOfHMetrics, ushort numGlyphs)
    {
        if (numGlyphs == 0 || numberOfHMetrics == 0)
        {
            return [];
        }

        var requiredMetricBytes = checked((uint)numberOfHMetrics * 4u);
        if (hmtxTable.Length < requiredMetricBytes)
        {
            throw new InvalidDataException("Invalid TrueType font: hmtx table is truncated.");
        }

        var list = new List<ushort>(numGlyphs);

        var pos = hmtxTable.Offset;
        for (var i = 0; i < numberOfHMetrics; i++)
        {
            list.Add(ReadUInt16At(data, pos));
            pos += 4;
        }

        var last = list.Count > 0 ? list[^1] : (ushort)0;
        while (list.Count < numGlyphs)
        {
            list.Add(last);
        }

        return list;
    }

    private static Dictionary<int, int> ReadCmap(byte[] data, TableRecord cmapTable)
    {
        var tableStart = cmapTable.Offset;
        var version = ReadUInt16At(data, tableStart);
        if (version != 0)
        {
            throw new NotSupportedException("Unsupported cmap version.");
        }

        var numTables = ReadUInt16At(data, tableStart + 2);
        var encodingRecordsLength = checked((uint)numTables * 8u);
        if (cmapTable.Length < 4 + encodingRecordsLength)
        {
            throw new InvalidDataException("Invalid TrueType font: cmap encoding records are truncated.");
        }

        var bestSubtableOffset = -1;
        var bestSubtableLength = -1;

        for (var i = 0; i < numTables; i++)
        {
            var tableBase = tableStart + (uint)(4 + i * 8);
            var platformId = ReadUInt16At(data, tableBase);
            var encodingId = ReadUInt16At(data, tableBase + 2);
            var subtableOffset = ReadUInt32At(data, tableBase + 4);

            if (subtableOffset > cmapTable.Length - 2)
            {
                continue;
            }

            var subtableStart = checked((int)(tableStart + subtableOffset));
            var format = ReadUInt16At(data, (uint)subtableStart);
            if (format != 4)
            {
                continue;
            }

            if (subtableOffset > cmapTable.Length - 4)
            {
                continue;
            }

            var length = ReadUInt16At(data, (uint)subtableStart + 2);
            if (length < 16 || subtableOffset + length > cmapTable.Length)
            {
                throw new InvalidDataException("Invalid TrueType font: cmap subtable length is out of range.");
            }

            if (platformId == 3 && encodingId == 1)
            {
                bestSubtableOffset = subtableStart;
                bestSubtableLength = length;
                break;
            }
        }

        if (bestSubtableOffset < 0)
        {
            throw new NotSupportedException("No supported cmap subtable found.");
        }

        return ReadCmapFormat4(data, bestSubtableOffset, bestSubtableLength);
    }

    private static Dictionary<int, int> ReadCmapFormat4(byte[] data, int offset, int length)
    {
        var segCount = ReadUInt16At(data, (uint)offset + 6) / 2;
        if (segCount == 0)
        {
            throw new InvalidDataException("Invalid TrueType font: cmap format 4 has no segments.");
        }

        var segCountX2 = ReadUInt16At(data, (uint)offset + 6);
        if ((segCountX2 & 1) != 0)
        {
            throw new InvalidDataException("Invalid TrueType font: cmap format 4 segCountX2 is invalid.");
        }

        var endCountOffset = checked(offset + 14);
        var startCountOffset = checked(endCountOffset + 2 + segCount * 2);
        var idDeltaOffset = checked(startCountOffset + segCount * 2);
        var idRangeOffsetOffset = checked(idDeltaOffset + segCount * 2);
        var glyphArrayOffset = checked(idRangeOffsetOffset + segCount * 2);
        if (glyphArrayOffset > offset + length)
        {
            throw new InvalidDataException("Invalid TrueType font: cmap format 4 arrays overflow declared length.");
        }

        var map = new Dictionary<int, int>();
        const int minCode = 32;
        const int maxCode = 126;

        for (var i = 0; i < segCount; i++)
        {
            var endCode = ReadUInt16At(data, (uint)(endCountOffset + i * 2));
            var startCode = ReadUInt16At(data, (uint)(startCountOffset + i * 2));
            var idDelta = ReadInt16At(data, (uint)(idDeltaOffset + i * 2));
            var idRangeOffset = ReadUInt16At(data, (uint)(idRangeOffsetOffset + i * 2));

            var endCodeInt = endCode;
            var startCodeInt = startCode;

            if (endCodeInt < minCode || startCodeInt > maxCode)
            {
                continue;
            }

            var from = Math.Max((int)startCodeInt, minCode);
            var to = Math.Min((int)endCodeInt, maxCode);

            for (var code = from; code <= to; code++)
            {
                if (code == 0xFFFF)
                {
                    continue;
                }

                int glyphIndex;
                if (idRangeOffset == 0)
                {
                    glyphIndex = (code + idDelta) & 0xFFFF;
                }
                else
                {
                    var roffset = checked(idRangeOffsetOffset + i * 2);
                    var glyphIndexOffset = checked(roffset + idRangeOffset + (code - startCodeInt) * 2);
                    if (glyphIndexOffset < offset || glyphIndexOffset + 2 > offset + length)
                    {
                        throw new InvalidDataException("Invalid TrueType font: cmap format 4 glyph index offset is out of range.");
                    }

                    glyphIndex = ReadUInt16At(data, (uint)glyphIndexOffset);
                    if (glyphIndex != 0)
                    {
                        glyphIndex = (glyphIndex + idDelta) & 0xFFFF;
                    }
                }

                map[code] = glyphIndex;
            }
        }

        return map;
    }

    private static string ReadPostScriptName(byte[] data, TableRecord? nameTable)
    {
        if (nameTable.HasValue)
        {
            EnsureMinimumLength(nameTable.Value, 6, "name");
            var offset = nameTable.Value.Offset;
            var tableLength = nameTable.Value.Length;
            var count = ReadUInt16At(data, offset + 2);
            var stringOffset = ReadUInt16At(data, offset + 4);
            var recordsLength = checked((uint)count * 12u);
            if (tableLength < 6 + recordsLength)
            {
                throw new InvalidDataException("Invalid TrueType font: name table records are truncated.");
            }

            for (var i = 0; i < count; i++)
            {
                var recordOffset = offset + (uint)(6 + i * 12);
                var platformId = ReadUInt16At(data, recordOffset);
                var encodingId = ReadUInt16At(data, recordOffset + 2);
                _ = ReadUInt16At(data, recordOffset + 4);
                var nameId = ReadUInt16At(data, recordOffset + 6);
                var length = ReadUInt16At(data, recordOffset + 8);
                var recordStringOffset = ReadUInt16At(data, recordOffset + 10);

                if (nameId != 6)
                {
                    continue;
                }

                var stringPos = checked((int)(offset + stringOffset + recordStringOffset));
                EnsureRange(data, (uint)stringPos, length, "name table string");
                if (platformId == 3 && encodingId == 1)
                {
                    if ((length & 1) != 0)
                    {
                        throw new InvalidDataException("Invalid TrueType font: UTF-16 name string length is odd.");
                    }

                    return Encoding.BigEndianUnicode.GetString(data.AsSpan(stringPos, length)).Replace(" ", string.Empty);
                }

                if (platformId == 1)
                {
                    return Encoding.ASCII.GetString(data.AsSpan(stringPos, length)).Replace(" ", string.Empty);
                }
            }
        }

        var fallback = $"TTF{Guid.NewGuid():N}";
        return fallback;
    }

    private static TableRecord RequireTable(IReadOnlyDictionary<string, TableRecord> tables, string tag)
    {
        if (!tables.TryGetValue(tag, out var record))
        {
            throw new NotSupportedException($"Missing required TrueType table '{tag}'.");
        }

        return record;
    }

    private static ushort ReadUInt16At(byte[] data, uint offset)
    {
        EnsureRange(data, offset, 2, "UInt16");
        return BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan((int)offset, 2));
    }

    private static uint ReadUInt32At(byte[] data, uint offset)
    {
        EnsureRange(data, offset, 4, "UInt32");
        return BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan((int)offset, 4));
    }

    private static short ReadInt16At(byte[] data, uint offset)
    {
        EnsureRange(data, offset, 2, "Int16");
        return BinaryPrimitives.ReadInt16BigEndian(data.AsSpan((int)offset, 2));
    }

    private static double ReadFixedAt(byte[] data, uint offset)
    {
        EnsureRange(data, offset, 4, "Fixed");
        var value = BinaryPrimitives.ReadInt32BigEndian(data.AsSpan((int)offset, 4));
        return value / 65536.0;
    }

    private static void EnsureMinimumLength(TableRecord table, uint minLength, string tag)
    {
        if (table.Length < minLength)
        {
            throw new InvalidDataException($"Invalid TrueType font: table '{tag}' is too short.");
        }
    }

    private static void EnsureRange(byte[] data, uint offset, uint length, string context)
    {
        if (offset > int.MaxValue)
        {
            throw new InvalidDataException($"Invalid TrueType font: {context} offset is out of range.");
        }

        if (length > int.MaxValue)
        {
            throw new InvalidDataException($"Invalid TrueType font: {context} length is out of range.");
        }

        if ((ulong)offset + length > (ulong)data.Length)
        {
            throw new InvalidDataException($"Invalid TrueType font: {context} is out of bounds.");
        }
    }

    private readonly record struct TableRecord(uint Offset, uint Length);

    private ref struct BigEndianReader
    {
        private readonly ReadOnlySpan<byte> _data;
        private int _offset;

        public BigEndianReader(ReadOnlySpan<byte> data)
        {
            _data = data;
            _offset = 0;
        }

        public void Skip(int count)
        {
            if (count < 0)
            {
                throw new InvalidDataException("Invalid TrueType font: negative skip.");
            }

            EnsureAvailable(count, "skip");
            _offset += count;
        }

        public ushort ReadUInt16()
        {
            EnsureAvailable(2, "UInt16");
            var value = BinaryPrimitives.ReadUInt16BigEndian(_data.Slice(_offset, 2));
            _offset += 2;
            return value;
        }

        public uint ReadUInt32()
        {
            EnsureAvailable(4, "UInt32");
            var value = BinaryPrimitives.ReadUInt32BigEndian(_data.Slice(_offset, 4));
            _offset += 4;
            return value;
        }

        public string ReadTag()
        {
            EnsureAvailable(4, "tag");
            var tag = Encoding.ASCII.GetString(_data.Slice(_offset, 4));
            _offset += 4;
            return tag;
        }

        private readonly void EnsureAvailable(int count, string context)
        {
            if (count < 0 || _offset > _data.Length - count)
            {
                throw new InvalidDataException($"Invalid TrueType font: unable to read {context}.");
            }
        }
    }
}
