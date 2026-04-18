using MarkdownToPdf.Core.Document;

namespace MarkdownToPdf.Core.Layout;

internal sealed partial class TableLayoutComponent : ILayoutComponent
{
    public bool TryPrepare(IDocumentElement element, LayoutContext context)
    {
        if (element is not Table table)
        {
            return false;
        }

        context.SetPlan(element, BuildTablePlan(table, context.Theme.BodyFontSize, context));
        return true;
    }

    public bool TryLayout(IDocumentElement element, LayoutContext context)
    {
        if (element is not Table table)
        {
            return false;
        }

        if (!context.TryGetPlan<TableLayoutPlan>(element, out var plan))
        {
            plan = BuildTablePlan(table, context.Theme.BodyFontSize, context);
        }

        AddTable(table, context, plan);
        return true;
    }

    private static TableLayoutPlan BuildTablePlan(Table table, double fontSize, LayoutContext context)
    {
        var columnCount = table.Rows.Max(row => row.Cells.Count);
        var columnWidths = new double[columnCount];
        var cellPadding = table.CellPadding;
        var lineHeight = fontSize * 1.2;
        Parallel.For(0, columnCount, col =>
        {
            var maxWidth = 0.0;
            for (var rowIndex = 0; rowIndex < table.Rows.Count; rowIndex++)
            {
                var row = table.Rows[rowIndex];
                if (col >= row.Cells.Count)
                {
                    continue;
                }

                var width = MeasureInlineRuns(row.Cells[col].Inlines, fontSize, row.IsHeader ? TextStyle.Bold : (TextStyle?)null, context);
                if (width > maxWidth)
                {
                    maxWidth = width;
                }
            }

            columnWidths[col] = maxWidth + (cellPadding * 2);
        });

        var rowLayouts = BuildTableRowLayouts(table, columnCount, columnWidths, fontSize, cellPadding, lineHeight, context);
        return new TableLayoutPlan(columnWidths, rowLayouts, columnWidths.Sum(), lineHeight, rowLayouts.Sum(layout => layout.Height) + context.Theme.BlockSpacing + (lineHeight * 0.8), fontSize);
    }

    private static void AddTable(Table table, LayoutContext context, TableLayoutPlan plan)
    {
        using var _profileScope = LayoutProfiling.Scope(LayoutProfilePoint.AddTable);
        var columnCount = plan.ColumnWidths.Length;
        var columnWidths = plan.ColumnWidths;
        var cellPadding = table.CellPadding;
        var lineHeight = plan.LineHeight;
        var alignments = table.ColumnAlignments;
        var defaultVerticalAlignment = table.DefaultCellVerticalAlignment;
        var hasHorizontalBorders = table.BorderStyle is TableBorderStyle.Grid or TableBorderStyle.Horizontal;
        var hasVerticalBorders = table.BorderStyle is TableBorderStyle.Grid or TableBorderStyle.Vertical;
        var rowLayouts = plan.RowLayouts;
        var requiredHeight = plan.RequiredHeight;
        if (requiredHeight <= context.PageSize.Height - context.Margins.Top - context.Margins.Bottom && requiredHeight > context.Y - context.Margins.Bottom)
        {
            context.CommitPage();
        }

        var tableTop = context.Y;
        var tableBottom = tableTop;
        var totalWidth = plan.TotalWidth;
        foreach (var rowLayout in rowLayouts)
        {
            var row = rowLayout.Row;
            var rowTop = context.Y;
            var rowBottom = context.Y - rowLayout.Height;

            if (rowBottom - context.Theme.BlockSpacing < context.Margins.Bottom)
            {
                context.CommitPage();
                tableTop = context.Y;
                rowTop = context.Y;
                rowBottom = context.Y - rowLayout.Height;
            }

            if (hasHorizontalBorders && tableBottom == tableTop)
            {
                context.CurrentItems.Add(new LayoutLine(context.Margins.Left, rowTop, context.Margins.Left + totalWidth, rowTop, table.BorderThickness));
            }

            var currentX = context.Margins.Left;
            var baselineFirst = context.Y - cellPadding - (lineHeight * 0.8);
            for (var col = 0; col < columnCount; col++)
            {
                if (col < row.Cells.Count)
                {
                    var available = Math.Max(0, columnWidths[col] - cellPadding * 2);
                    var wrappedLines = rowLayout.Lines[col];
                    var maxContentWidth = rowLayout.MaxContentWidths[col];

                    var offset = (col < alignments.Count ? alignments[col] : TableColumnAlignment.Left) switch
                    {
                        TableColumnAlignment.Center => Math.Max(0, (available - maxContentWidth) / 2),
                        TableColumnAlignment.Right => Math.Max(0, available - maxContentWidth),
                        _ => 0
                    };

                    var startY = baselineFirst - ((row.Cells[col].VerticalAlignment ?? defaultVerticalAlignment) switch
                    {
                        TableCellVerticalAlignment.Middle => (rowLayout.MaxLines - wrappedLines.Count) / 2.0,
                        TableCellVerticalAlignment.Bottom => rowLayout.MaxLines - wrappedLines.Count,
                        _ => 0
                    }) * lineHeight;
                    for (var lineIndex = 0; lineIndex < wrappedLines.Count; lineIndex++)
                    {
                        AddCellText(
                            wrappedLines[lineIndex].Runs,
                            plan.FontSize,
                            currentX + cellPadding + offset,
                            startY - lineIndex * lineHeight,
                            context);
                    }
                }

                currentX += columnWidths[col];
            }

            if (hasHorizontalBorders)
            {
                context.CurrentItems.Add(new LayoutLine(context.Margins.Left, rowBottom, context.Margins.Left + totalWidth, rowBottom, table.BorderThickness));
            }

            if (hasVerticalBorders)
            {
                var x = context.Margins.Left;
                context.CurrentItems.Add(new LayoutLine(x, rowTop, x, rowBottom, table.BorderThickness));
                for (var col = 0; col < columnWidths.Length; col++)
                {
                    x += columnWidths[col];
                    context.CurrentItems.Add(new LayoutLine(x, rowTop, x, rowBottom, table.BorderThickness));
                }
            }

            context.Y = rowBottom;
            tableBottom = rowBottom;
        }

        if (hasVerticalBorders)
        {
            var x = context.Margins.Left;
            context.CurrentItems.Add(new LayoutLine(x, tableTop, x, tableBottom, table.BorderThickness));
            for (var col = 0; col < columnWidths.Length; col++)
            {
                x += columnWidths[col];
                context.CurrentItems.Add(new LayoutLine(x, tableTop, x, tableBottom, table.BorderThickness));
            }
        }

        context.Y -= context.Theme.BlockSpacing + (lineHeight * 0.8);
    }

    private static IReadOnlyList<TableRowLayout> BuildTableRowLayouts(
        Table table,
        int columnCount,
        double[] columnWidths,
        double fontSize,
        double cellPadding,
        double lineHeight,
        LayoutContext context)
    {
        var rowLayouts = new TableRowLayout[table.Rows.Count];
        Parallel.For(0, table.Rows.Count, rowIndex =>
        {
            var row = table.Rows[rowIndex];
            var rowCellLines = new List<IReadOnlyList<PreparedCellLine>>(columnCount);
            var maxContentWidths = new double[columnCount];
            var maxLines = 1;

            for (var col = 0; col < columnCount; col++)
            {
                if (col >= row.Cells.Count)
                {
                    rowCellLines.Add([new PreparedCellLine([], 0)]);
                    maxContentWidths[col] = 0;
                    continue;
                }

                var cell = row.Cells[col];
                var styleOverride = row.IsHeader ? TextStyle.Bold : (TextStyle?)null;
                var maxWidth = Math.Max(0, columnWidths[col] - (cellPadding * 2));
                var wrapped = TextLayoutComponent.WrapInlineRunsWithWidths(cell.Inlines, fontSize, maxWidth, styleOverride, context);
                var preparedLines = new PreparedCellLine[wrapped.Count];
                var maxContentWidth = 0.0;
                for (var lineIndex = 0; lineIndex < wrapped.Count; lineIndex++)
                {
                    var wrappedLine = wrapped[lineIndex];
                    var preparedRuns = new PreparedCellRun[wrappedLine.Inlines.Count];
                    for (var runIndex = 0; runIndex < wrappedLine.Inlines.Count; runIndex++)
                    {
                        var run = wrappedLine.Inlines[runIndex];
                        preparedRuns[runIndex] = new PreparedCellRun(
                            run.Text,
                            run.Style,
                            context.Measure(run.Text, run.Style, fontSize));
                    }

                    preparedLines[lineIndex] = new PreparedCellLine(preparedRuns, wrappedLine.Width);
                    var lineWidth = wrappedLine.Width;
                    if (lineWidth > maxContentWidth)
                    {
                        maxContentWidth = lineWidth;
                    }
                }

                rowCellLines.Add(preparedLines);
                maxContentWidths[col] = maxContentWidth;
                if (wrapped.Count > maxLines)
                {
                    maxLines = wrapped.Count;
                }
            }

            rowLayouts[rowIndex] = new TableRowLayout(row, rowCellLines, maxContentWidths, maxLines, (maxLines * lineHeight) + (cellPadding * 2));
        });

        return [.. rowLayouts];
    }

    private static double MeasureInlineRuns(IReadOnlyList<InlineRun> inlines, double fontSize, TextStyle? styleOverride, LayoutContext context)
    {
        var total = 0.0;
        for (var i = 0; i < inlines.Count; i++)
        {
            total += context.Measure(inlines[i].Text, styleOverride ?? inlines[i].Style, fontSize);
        }

        return total;
    }

    private static void AddCellText(
        IReadOnlyList<PreparedCellRun> runs,
        double fontSize,
        double startX,
        double baselineY,
        LayoutContext context)
    {
        var currentX = startX;
        for (var i = 0; i < runs.Count; i++)
        {
            var run = runs[i];
            context.CurrentItems.Add(new LayoutText(run.Text, currentX, baselineY, fontSize, run.Style));
            currentX += run.Width;
        }
    }
}
