using System;
using System.Linq;
using NUnit.Framework;

namespace RampUp.Tests
{
    public static class ExceptionHelpers
    {
        public static void ExceptionOrAggregateWithOne(Exception ex, Exception expected)
        {
            var e = ex as AggregateException;
            if (e != null)
            {
                Assert.AreEqual(1, e.InnerExceptions.Count);
                Assert.True(ReferenceEquals(e.InnerExceptions.Single(), expected));
            }
            else
            {
                Assert.True(ReferenceEquals(ex, expected));
            }
        }
    }
}