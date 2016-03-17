using System;

namespace RampUp.Actors.Time
{
    public interface IScheduler
    {
        /// <summary>
        /// Schedules delivery of <paramref name="message"/> after given <paramref name="timeout"/>.
        /// </summary>
        void Schedule<TMessage>(TimeSpan timeout, ref TMessage message)
            where TMessage : struct;
    }
}