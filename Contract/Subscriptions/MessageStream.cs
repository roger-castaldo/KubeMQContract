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
        protected bool disposedValue;

        public ulong Length => success+errors;

        public IStreamStats Stats => new StreamStats()
        {
            Errors=errors,
            Success=success
        };

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    errors=0;
                    success=0;
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue=true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~MessageStream()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
