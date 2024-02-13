
using Google.Protobuf.Collections;
using KubeMQ.Contract.Interfaces.Messages;

namespace KubeMQ.Contract.Messages
{
    internal class Message<T> : IInternalMessage<T> 
    {
        private bool disposedValue;
        public T? Data { get; private init; }
        public DateTime Timestamp { get; private init; } = DateTime.Now;
        public DateTime ConversionTimestamp { get; private init; }=DateTime.Now;

        public string ID { get; private init; } = string.Empty;

        public Exception? Exception { get; private init; } = null;

        public IMessageHeader Headers { get; private init; }

        public Message(string id,T? data=default,DateTime? timestamp=null,DateTime? conversionTimestamp=null, MapField<string, string>? tags = null,Exception exception=null)
        {
            ID=id;
            Data=data;
            Timestamp=timestamp??DateTime.Now;
            ConversionTimestamp=conversionTimestamp??DateTime.Now;
            Headers = new ReadonlyMessageHeader(tags);
            Exception=exception;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                    if (typeof(T).GetInterfaces().Any(t => t==typeof(IDisposable)) && Data!=null)
                    {
                        try
                        {
                            ((IDisposable)Data).Dispose();
                        }
                        catch (Exception) { }
                    }
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue=true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~Message()
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
