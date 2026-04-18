using System.Buffers;
using System.Globalization;
using System.Text;

namespace MarkdownToPdf.Core.Rendering;

internal sealed partial class PdfWriter
{
    /// <summary>
    /// Growable byte buffer backed by <see cref="ArrayPool{Byte}"/>. Must either be
    /// disposed or detached (via <see cref="Detach"/>) exactly once to avoid leaking
    /// the rented array back to the pool.
    /// </summary>
    private ref struct PooledByteBuilder
    {
        private byte[]? _buffer;
        private int _length;

        public PooledByteBuilder(int initialCapacity)
        {
            _buffer = ArrayPool<byte>.Shared.Rent(Math.Max(initialCapacity, 128));
            _length = 0;
        }

        public int Length => _length;

        public void AppendByte(byte value)
        {
            EnsureCapacity(1);
            _buffer![_length++] = value;
        }

        public void AppendAscii(scoped ReadOnlySpan<byte> utf8Literal)
        {
            EnsureCapacity(utf8Literal.Length);
            utf8Literal.CopyTo(_buffer!.AsSpan(_length));
            _length += utf8Literal.Length;
        }

        public void AppendAscii(string ascii)
        {
            EnsureCapacity(ascii.Length);
            _length += Encoding.ASCII.GetBytes(ascii, _buffer!.AsSpan(_length));
        }

        public void AppendAscii(scoped ReadOnlySpan<char> ascii)
        {
            EnsureCapacity(ascii.Length);
            _length += Encoding.ASCII.GetBytes(ascii, _buffer!.AsSpan(_length));
        }

        public void AppendInt(long value)
        {
            EnsureCapacity(24);
            value.TryFormat(_buffer!.AsSpan(_length), out var written, default, CultureInfo.InvariantCulture);
            _length += written;
        }

        public void AppendDouble(double value)
        {
            EnsureCapacity(32);
            value.TryFormat(_buffer!.AsSpan(_length), out var written, "0.###", CultureInfo.InvariantCulture);
            _length += written;
        }

        /// <summary>
        /// Releases ownership of the underlying buffer. The caller takes over the
        /// responsibility to return it to the pool.
        /// </summary>
        public PooledBuffer Detach()
        {
            var buffer = _buffer ?? throw new ObjectDisposedException(nameof(PooledByteBuilder));
            var result = new PooledBuffer(buffer, _length);
            _buffer = null;
            _length = 0;
            return result;
        }

        /// <summary>
        /// Returns the rented buffer to the pool if the builder still owns one.
        /// Safe to call multiple times and after <see cref="Detach"/>.
        /// </summary>
        public void Dispose()
        {
            if (_buffer is not null)
            {
                ArrayPool<byte>.Shared.Return(_buffer);
                _buffer = null;
                _length = 0;
            }
        }

        private void EnsureCapacity(int additional)
        {
            var buffer = _buffer ?? throw new ObjectDisposedException(nameof(PooledByteBuilder));
            var required = _length + additional;
            if (required <= buffer.Length)
            {
                return;
            }

            var newSize = Math.Max(buffer.Length * 2, required);
            var newBuffer = ArrayPool<byte>.Shared.Rent(newSize);
            Buffer.BlockCopy(buffer, 0, newBuffer, 0, _length);
            ArrayPool<byte>.Shared.Return(buffer);
            _buffer = newBuffer;
        }
    }
}
