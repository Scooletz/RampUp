using System;
using System.Collections.Generic;
using System.Linq;
using NSubstitute;
using NUnit.Framework;
using RampUp.Actors;
using RampUp.Actors.Impl;
using RampUp.Buffers;
using RampUp.Ring;

namespace RampUp.Tests.Actors.Impl
{
    public class RunnerTests
    {
        private static readonly Dictionary<Type, int> ids = new Dictionary<Type, int>
        {
            {typeof (A), MessageAId},
            {typeof (B), MessageBId},
        };

        private const int MessageAId = 1;
        private const int MessageBId = 2;

        [Test]
        public void When_spinned_once_then_dispatches_one_batch()
        {
            const int batchSize = 10;
            const int aCount = 3;
            const int bCount = 4;
            var buffer = Substitute.For<IRingBuffer>();
            var aHandler = new AHandler();
            var bHandler = new BHandler();

            buffer.Read(Arg.Any<MessageHandler>(), Arg.Is(batchSize)).Returns(ci =>
            {
                var handler = ci.ArgAt<MessageHandler>(0);
                for (var i = 0; i < aCount; i++)
                {
                    handler(MessageAId, ByteChunk.Empty);
                }

                for (var i = 0; i < bCount; i++)
                {
                    handler(MessageBId, ByteChunk.Empty);
                }

                return aCount + bCount;
            });

            var runner = new Runner(buffer, new StructSizeCounter(), t => ids[t], batchSize, aHandler, bHandler);
            BatchInfo batch;
            runner.SpinOnce(out batch);

            Assert.AreEqual(1, buffer.ReceivedCalls().Count());
            Assert.AreEqual(batchSize, batch.RequestedNumberOfMessages);
            Assert.AreEqual(aCount + bCount, batch.ProcessedNumberOfMessages);
            Assert.Less(0, batch.StopWatchTicksSpentOnProcessing);
            Assert.AreEqual(aCount, aHandler.Counter);
            Assert.AreEqual(bCount, bHandler.Counter);
            Assert.True(aHandler.BatchFinished);
            Assert.True(bHandler.BatchFinished);
        }

        public class AHandler : IHandle<A>, IBatchAware
        {
            public int Counter;
            public bool BatchFinished;

            public void Handle(ref Envelope envelope, ref A msg)
            {
                Counter += 1;
            }

            public void OnBatchEnded(ref BatchInfo info)
            {
                BatchFinished = true;
            }
        }

        public class BHandler : IHandle<B>, IBatchAware
        {
            public int Counter;
            public bool BatchFinished;

            public void Handle(ref Envelope envelope, ref B msg)
            {
                Counter += 1;
            }

            public void OnBatchEnded(ref BatchInfo info)
            {
                BatchFinished = true;
            }
        }

        public struct A
        {
        }

        public struct B
        {
        }
    }
}