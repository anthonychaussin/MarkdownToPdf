using System.Diagnostics;
using System.Text;

namespace MarkdownToPdf.Core.Layout;

public static partial class LayoutProfiling
{
    private static readonly long[] TickCounters = new long[4];
    private static readonly long[] CallCounters = new long[4];

    public static bool Enabled { get; set; }

    public static void Reset()
    {
        Array.Clear(TickCounters);
        Array.Clear(CallCounters);
    }

    public static ScopeToken Scope(LayoutProfilePoint point)
    {
        if (!Enabled)
        {
            return default;
        }

        return new ScopeToken((int)point, Stopwatch.GetTimestamp(), enabled: true);
    }

    public static string BuildReport()
    {
        var totalTicks = TickCounters.Sum();
        var builder = new StringBuilder();
        builder.AppendLine("Layout profiling (internal):");
        foreach (var (point, ticks, calls) in from LayoutProfilePoint point in Enum.GetValues<LayoutProfilePoint>()
                                              let i = (int)point
                                              select (point, TickCounters[i], CallCounters[i]))
        {
            if (calls == 0)
            {
                continue;
            }

            var ms = ticks * 1000.0 / Stopwatch.Frequency;
            var avgUs = (ticks * 1_000_000.0 / Stopwatch.Frequency) / calls;
            var pct = totalTicks > 0 ? (ticks * 100.0 / totalTicks) : 0;
            builder.Append(point)
                .Append(": calls=").Append(calls)
                .Append(", total=").Append(ms.ToString("0.###")).Append(" ms")
                .Append(", avg=").Append(avgUs.ToString("0.###")).Append(" us")
                .Append(", share=").Append(pct.ToString("0.##")).AppendLine("%");
        }

        return builder.ToString();
    }
}
