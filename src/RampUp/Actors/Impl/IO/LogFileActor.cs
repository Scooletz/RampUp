using System;

namespace RampUp.Actors.Impl.IO
{
    internal class LogFileActor : IHandle<LogFile.AppendRequest>
    {
        private IntPtr File;

        public LogFileActor(CompletionPortIdleStrategy completion)
        {
            completion.Register(File, Completed, FlushFileAtTheEndOfDispatch);
        }

        private void Completed(CompletionPortIdleStrategy.CompletedAction obj)
        {
            throw new NotImplementedException();
        }

        public void Handle(ref Envelope envelope, ref LogFile.AppendRequest msg)
        {
        }

        private void FlushFileAtTheEndOfDispatch()
        {
            throw new NotImplementedException();
        }
    }
}