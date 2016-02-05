using System;
using System.Collections.Generic;

namespace RampUp.Tests.Actors
{
    public class FakeMessageIdGenerator
    {
        private static readonly Dictionary<Type, int> Values = new Dictionary<Type, int>();

        public static int GetMessageId(Type t)
        {
            lock (Values)
            {
                int id;
                if (Values.TryGetValue(t, out id) == false)
                {
                    id = Values.Count + 1;
                    Values[t] = id;
                }
                return id;
            }
        }
    }
}