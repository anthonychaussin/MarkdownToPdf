using System.Diagnostics;

namespace MarkdownToPdf.Core.Layout;

public static partial class LayoutProfiling
{
    public readonly struct ScopeToken : IDisposable
    {
        private readonly int _index;
        private readonly long _start;
        private readonly bool _enabled;

        internal ScopeToken(int index, long start, bool enabled)
        {
            _index = index;
            _start = start;
            _enabled = enabled;
        }

        public void Dispose()
        {
            if (!_enabled)
            {
                return;
            }

            Interlocked.Add(ref TickCounters[_index], Stopwatch.GetTimestamp() - _start);
            Interlocked.Increment(ref CallCounters[_index]);
        }
    }
}
