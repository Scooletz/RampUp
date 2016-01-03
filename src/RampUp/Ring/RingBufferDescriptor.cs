using System;
using RampUp.Buffers;

namespace RampUp.Ring
{
    public class RingBufferDescriptor
    {
        /// <summary>
        /// Message type is padding to prevent fragmentation in the buffer
        /// </summary>
        public static readonly int PaddingMsgTypeId = -1;

        /// <summary>
        /// Offset within the record at which the record length field begins.
        /// </summary>
        public static readonly int LengthOffset = 0;

        /// <summary>
        /// Offset within the record at which the message type field begins.
        /// </summary>
        public static readonly int TypeOffset = LengthOffset + sizeof(int);

        /// <summary>
        /// Length of the record header in bytes.
        /// </summary>
        public static readonly int HeaderLength = sizeof(int) * 2;

        /// <summary>
        /// Alignment as a multiple of bytes for each record.L
        /// </summary>
        public static readonly int RecordAlignment = HeaderLength;

        /// <summary>
        /// Offset within the trailer for where the tail value is stored.
        /// </summary>
        public static readonly int TailPositionOffset;

        /// <summary>
        /// Offset within the trailer for where the head cache value is stored.
        /// </summary>
        public static readonly int HeadCachePositionOffset;

        /// <summary>
        /// Offset within the trailer for where the head value is stored.
        /// </summary>
        public static readonly int HeadPositionOffset;

        /// <summary>
        /// Offset within the trailer for where the correlation counter value is stored.
        /// </summary>
        public static readonly int CorrelationCounterOffset;

        /// <summary>
        /// Offset within the trailer for where the consumer heartbeat time value is stored.
        /// </summary>
        public static readonly int ConsumerHeartbeatOffset;

        /// <summary>
        /// Total length of the trailer in bytes.
        /// </summary>
        public static readonly int TrailerLength;

        static RingBufferDescriptor()
        {
            var offset = 0;
            offset += Native.CacheLineSize * 2;
            TailPositionOffset = offset;

            offset += Native.CacheLineSize * 2;
            HeadCachePositionOffset = offset;

            offset += Native.CacheLineSize * 2;
            HeadPositionOffset = offset;

            offset += Native.CacheLineSize * 2;
            CorrelationCounterOffset = offset;

            offset += Native.CacheLineSize * 2;
            ConsumerHeartbeatOffset = offset;

            offset += Native.CacheLineSize * 2;
            TrailerLength = offset;
        }

        /**
         * Check the the buffer capacity is the correct size (a power of 2 + {@link RingBufferDescriptor#TrailerLength}).
         *
         * @param capacity to be checked.
         * @throws IllegalStateException if the buffer capacity is incorrect.
         */

            ///<summary>
            /// Checks the buffer capacity
            /// </summary>
        public static void EnsureCapacity(IUnsafeBuffer buffer)
        {
            if ((buffer.Size - TrailerLength).IsPowerOfTwo() == false)
            {
                throw new ArgumentException("Size must be a positive power of 2 + TrailerLength, but size=" + buffer.Size);
            }
        }

        public static void ValidateMessageTypeId(int messageTypeId)
        {
            if (messageTypeId < 1)
            {
                throw new ArgumentException($"The message type must be greater than zero. It was {messageTypeId}");
            }
        }

        public static long MakeHeader(int length, int messageTypeId)
        {
            return ((long)messageTypeId << 32) | length;
        }

        public static int RecordLength(long header)
        {
            return (int)header;
        }

        public static int MessageTypeId(long header)
        {
            return (int)(header >> 32);
        }

        public static int EncodedMsgOffset(int recordOffset)
        {
            return recordOffset + HeaderLength;
        }
    }
}