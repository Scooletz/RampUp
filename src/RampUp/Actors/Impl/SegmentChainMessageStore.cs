using System.Runtime.InteropServices;
using RampUp.Buffers;
using RampUp.Ring;

namespace RampUp.Actors.Impl
{
    /// <summary>
    /// A class storying & reading messages from segments chains. May be used to accumulate messages
    /// </summary>
    public unsafe class SegmentChainMessageStore
    {
        private const int SegmentHeaderSize = 8;
        private readonly IMessageWriter _writer;
        private readonly int _segmentLength;
        private readonly ISegmentPool _pool;

        private Segment* _currentHead;

        public SegmentChainMessageStore(IMessageWriter writer, ISegmentPool pool)
        {
            _writer = writer;
            _pool = pool;
            _segmentLength = _pool.SegmentLength;
        }

        public void Write<TMessage>(ref Envelope envelope, ref TMessage message, ref Segment* head)
            where TMessage : struct
        {
            if (head == null)
            {
                head = PopNew();
            }

            _currentHead = head;
            _writer.Write(ref envelope, ref message, Write);
        }

        public void Consume(MessageHandler handler, ref Segment* head)
        {
            var current = head;
            while (current != null)
            {
                var firstAvailablePosition = *(long*) current->Buffer;
                var offset = SegmentHeaderSize;
                while (offset < firstAvailablePosition)
                {
                    var header = current->Buffer + offset;
                    var messageHeader = (Header*) header;
                    handler(messageHeader->MessageTypeId,
                        new ByteChunk(header + sizeof (Header), messageHeader->ChunkLength));

                    offset += GetNeededBytes(messageHeader->ChunkLength);
                }

                current = current->Next;
            }

            if (head != null)
            {
                _pool.Push(head);
                head = null;
            }
        }

        private bool Write(int messagetypeid, ByteChunk chunk)
        {
            var neededBytes = GetNeededBytes(chunk.Length);
            var tail = _currentHead->Tail;

            // ReSharper disable once ConvertIfStatementToNullCoalescingExpression
            if (tail == null)
            {
                tail = _currentHead;
            }

            if (TryWriteToSegment(messagetypeid, chunk, tail, neededBytes))
            {
                return true;
            }

            // no write, pop new segment
            tail->Next = PopNew();

            return TryWriteToSegment(messagetypeid, chunk, tail->Next, neededBytes);
        }

        private static int GetNeededBytes(int length)
        {
            return (sizeof (Header) + length).AlignToMultipleOf(sizeof (long));
        }

        private Segment* PopNew()
        {
            var h = _pool.Pop();
            *(long*) h->Buffer = SegmentHeaderSize;
            return h;
        }

        private bool TryWriteToSegment(int messagetypeid, ByteChunk chunk, Segment* segment,
            int neededBytes)
        {
            var firstAvailablePosition = (long*) segment->Buffer;

            if (*firstAvailablePosition + neededBytes <= _segmentLength)
            {
                var buffer = segment->Buffer + *firstAvailablePosition;
                var header = (Header*) buffer;
                header->MessageTypeId = messagetypeid;
                header->ChunkLength = chunk.Length;

                Native.MemcpyUnmanaged(buffer + sizeof (Header), chunk.Pointer, chunk.Length);
                *firstAvailablePosition += neededBytes;

                return true;
            }

            return false;
        }

        [StructLayout(LayoutKind.Explicit, Size = 8)]
        private struct Header
        {
            [FieldOffset(0)] public int MessageTypeId;
            [FieldOffset(4)] public int ChunkLength;
        }
    }
}