using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Padded.Fody;
using RampUp.Buffers;

namespace RampUp.Ring
{
    /// <summary>
    /// Provides a simple poller for a reader that accepts streams of messages instead of using Ramp Up actor system.
    /// </summary>
    [Padded]
    public sealed class RingBufferPoller
    {
        private readonly IRingBuffer _buffer;
        private readonly HandleMessageAsStream _handler;
        private readonly ReadonlyByteChunkStream _stream;
        private const int MessageLimit = 100;

        public delegate void HandleMessageAsStream(int messageTypeId, Stream payload);

        private RingBufferPoller(IRingBuffer buffer, HandleMessageAsStream handler)
        {
            _buffer = buffer;
            _handler = handler;
            _stream = new ReadonlyByteChunkStream();
        }

        public static Task Start(IRingBuffer buffer, HandleMessageAsStream handler, CancellationToken token)
        {
            var poller = new RingBufferPoller(buffer, handler);
            return Task.Factory.StartNew(() => poller.Loop(token), token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        private void Loop(CancellationToken token)
        {
            MessageHandler handler = Handle;
            var wait = new SpinWait();
            while (token.IsCancellationRequested == false)
            {
                if (_buffer.Read(handler, MessageLimit) == 0)
                {
                    wait.SpinOnce();
                }
            }
        }

        private void Handle(int messageTypeId, ByteChunk chunk)
        {
            _stream.Wrap(chunk);
            _handler(messageTypeId, _stream);
        }
    }
}