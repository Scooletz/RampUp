namespace RampUp.Actors
{
    public interface IBatchAware
    {
        /// <summary>
        /// Called when dispatching a batch of messages for an actor has ended.
        /// </summary>
        void OnBatchEnded(ref BatchInfo info);
    }
}