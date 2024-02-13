using KubeMQ.Contract.Interfaces;

namespace KubeMQ.Contract.Messages
{
    internal class TransmissionResult : ITransmissionResult
    {
        protected bool disposedValue;

        public Guid MessageID { get; init; } = Guid.Empty;

        public bool IsError { get; init; } = false;

        public string? Error { get; init; } = String.Empty;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue=true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~TransmissionResult()
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
