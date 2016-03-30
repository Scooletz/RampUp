using System.Linq;
using NUnit.Framework;
using RampUp.Actors.Impl;

namespace RampUp.Tests.Actors.Impl
{
    public class IntLookupTests
    {
        [Test]
        public void Test()
        {
            var values = Enumerable.Range(1, 100).ToDictionary(i => i, i => i + 1);

            var intLookup = new IntLookup<int>(values.Keys.ToArray(), values.Values.ToArray());

            foreach (var kvp in values)
            {
                int value;
                if (intLookup.TryGet(kvp.Key, out value))
                {
                    Assert.AreEqual(kvp.Value, value);
                }
            }
        }
    }
}