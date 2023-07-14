using KubeMQ.Contract.Interfaces;

namespace KubeMQ.Contract.SDK
{
    internal class PingResult : IPingResult
    {
        private readonly KubeMQ.Contract.SDK.Grpc.PingResult result;
        private bool disposedValue;

        public PingResult(KubeMQ.Contract.SDK.Grpc.PingResult result)
        {
            this.result=result;
        }

        public string Host => this.result.Host;

        public string Version => this.result.Version;

        public DateTime ServerStartTime => Utility.FromUnixTime(this.result.ServerStartTime);

        public TimeSpan ServerUpTime => TimeSpan.FromSeconds(this.result.ServerUpTimeSeconds);

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
        // ~PingResult()
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
