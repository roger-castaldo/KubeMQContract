using KubeMQ.Contract.Interfaces.Messages;

namespace KubeMQ.Contract.Subscriptions
{
    internal abstract class MessageStream : IMessageStream
    {
        private class StreamStats : IStreamStats
        {
            public ulong Errors { get; init; }
            public ulong Success { get; init; }
        }

        protected ulong success = 0;
        protected ulong errors = 0;

        public ulong Length => success+errors;

        public IStreamStats Stats => new StreamStats()
        {
            Errors=errors,
            Success=success
        };

        public virtual void Dispose()
        {
            errors=0;
            success=0;
        }
    }
}
