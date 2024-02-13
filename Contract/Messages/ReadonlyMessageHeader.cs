using Google.Protobuf.Collections;
using KubeMQ.Contract.Interfaces.Messages;

namespace KubeMQ.Contract.Messages
{
    internal class ReadonlyMessageHeader : IMessageHeader
    {
        private bool disposedValue;

        internal MapField<string, string>? Tags { get; private init; }

        public ReadonlyMessageHeader(MapField<string,string>? tags=null) => Tags=tags;

        public IEnumerable<string> Keys => Tags?.Keys??Array.Empty<string>();

        public string? this[string key]
        {
            get
            {
                string? value = null;
                Tags?.TryGetValue(key, out value);
                return value;
            }
        }

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
        // ~ReadonlyMessageHeader()
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
