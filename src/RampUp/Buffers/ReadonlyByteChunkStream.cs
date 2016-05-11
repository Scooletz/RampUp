using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using Padded.Fody;

namespace RampUp.Buffers
{
    [Padded]
    public sealed class ReadonlyByteChunkStream : Stream
    {
        public void Wrap(ByteChunk chunk)
        {
            _chunk = chunk;
            _position = 0;
        }

        private ByteChunk _chunk;
        private long _position;

        public override bool CanRead => true;

        public override bool CanSeek => true;

        public override bool CanWrite => false;

        public override long Length => _chunk.Length;

        public long Capacity => _chunk.Length;

        /// <summary>Gets or sets the current position in a stream.</summary>
        /// <returns>The current position in the stream.</returns>
        /// <exception cref="T:System.ObjectDisposedException">The stream is closed.</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">The position is set to a value that is less than zero, or the position is larger than <see cref="F:System.Int32.MaxValue" /> or results in overflow when added to the current pointer.</exception>
        /// <filterpriority>2</filterpriority>
        public override long Position
        {
            get { return _position; }
            [SecuritySafeCritical]
            set
            {
                AssertPosition(value);
                _position = value;
            }
        }

        private void AssertPosition(long value)
        {
            if (value < 0L || value >= _chunk.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }
        }

        public override unsafe int Read([In, Out] byte[] buffer, int offset, int count)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));
            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset));
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));
            if (buffer.Length - offset < count)
                throw new ArgumentException("Offset length");
            var bytesToRead = _chunk.Length - _position;
            if (bytesToRead > count)
                bytesToRead = count;

            if (bytesToRead <= 0L)
                return 0;

            var len = (int)bytesToRead;

            if (len < 0)
                len = 0;

            Native.MemcpyFromUnmanaged(buffer, offset, _chunk.Pointer , (int) _position, len);
            _position += bytesToRead;
            return len;
        }

        /// <summary>Reads a byte from a stream and advances the position within the stream by one byte, or returns -1 if at the end of the stream.</summary>
        /// <returns>The unsigned byte cast to an <see cref="T:System.Int32" /> object, or -1 if at the end of the stream.</returns>
        /// <exception cref="T:System.ObjectDisposedException">The stream is closed.</exception>
        /// <exception cref="T:System.NotSupportedException">The underlying memory does not support reading.- or -The current position is at the end of the stream.</exception>
        /// <filterpriority>2</filterpriority>
        [SecuritySafeCritical]
        public override unsafe int ReadByte()
        {
            var index = _position;
            if (index >= _chunk.Length)
            {
                return -1;
            }
            _position += 1;
             return _chunk.Pointer[index];
        }

        public override void Flush()
        {
            throw new NotSupportedException();
        }

        public override long Seek(long offset, SeekOrigin loc)
        {
            long newPosition;
            switch (loc)
            {
                case SeekOrigin.Begin:
                    newPosition = offset;
                    break;
                case SeekOrigin.Current:
                    newPosition = _position + offset;
                    break;
                case SeekOrigin.End:
                    newPosition = _chunk.Length + offset;
                    break;
                default:
                    throw new ArgumentException(nameof(loc));
            }

            AssertPosition(newPosition);
            _position = newPosition;
            return _position;
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public override void WriteByte(byte value)
        {
            throw new NotSupportedException();
        }
    }
}