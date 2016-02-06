using System;
using System.Linq;
using NUnit.Framework;
using RampUp.Actors;
using RampUp.Actors.Impl;
using RampUp.Buffers;

namespace RampUp.Tests.Actors.Impl
{
    public unsafe class SegmentChainMessageStoreTests
    {
        private SingleThreadSegmentPool _pool;

        [SetUp]
        public void SetUp()
        {
            _pool = new SingleThreadSegmentPool(SingleThreadSegmentPool.MinimalSegmentCount);
        }

        [TearDown]
        public void TearDown()
        {
            _pool.Dispose();
            _pool = null;
        }

        [Test]
        public void When_multiple_messages_written_then_they_are_consumed_in_order_leaving_nothing()
        {
            var guidMessages = new GuidMessageReaderWriter();
            var store = new SegmentChainMessageStore(guidMessages, _pool);

            var lowerBoundEstimationOfMessagesPerSegment = SingleThreadSegmentPool.SegmentSize/sizeof (Guid);
            var atLeastTwoSegmentsOfMessages = lowerBoundEstimationOfMessagesPerSegment*2;

            var guids = Enumerable.Range(0, atLeastTwoSegmentsOfMessages).Select(i => Guid.NewGuid()).ToArray();
            var env = new Envelope();
            var head = default(Segment*);

            fixed (Guid* g = guids)
            {
                for (var i = 0; i < guids.Length; i++)
                {
                    store.Write(ref env, ref *(g + i), ref head);
                }
            }

            store.Consume(guidMessages.MessageHandler, ref head);
            CollectionAssert.AreEquivalent(guids, guidMessages.Received);

            // nothing is left
            guidMessages.Received.Clear();

            store.Consume(guidMessages.MessageHandler, ref head);
            CollectionAssert.IsEmpty(guidMessages.Received);
        }
    }
}