namespace RampUp.Actors
{
    /// <summary>
    /// Data for a single round of disp
    /// </summary>
    public struct BatchInfo
    {
        public readonly int RequestedNumberOfMessages;
        public readonly int ProcessedNumberOfMessages;
        public readonly long StopWatchTicksSpentOnProcessing;

        public BatchInfo(int requestedNumberOfMessages, int processedNumberOfMessages,
            long stopWatchTicksSpentOnProcessing)
        {
            RequestedNumberOfMessages = requestedNumberOfMessages;
            ProcessedNumberOfMessages = processedNumberOfMessages;
            StopWatchTicksSpentOnProcessing = stopWatchTicksSpentOnProcessing;
        }
    }
}