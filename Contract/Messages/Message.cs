
using KubeMQ.Contract.Interfaces.Messages;

namespace KubeMQ.Contract.Messages
{
    internal class Message<T> : TransmittedMessage, IInternalMessage<T> 
    {
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public T Data { get; init; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public DateTime Timestamp { get; init; } = DateTime.Now;
        public DateTime ConversionTimestamp { get; init; }=DateTime.Now;

        public string ID { get; init; } = string.Empty;

        public Exception? Exception { get; init; } = null;

        public new string? Error { 
            get {
                return Exception?.Message??base.Error;
            }
            init
            {
                base.Error=value;
            }
        }

        public new bool IsError
        {
            get
            {
                return Exception!=null||base.IsError;
            }
            init
            {
                base.IsError=value;
            }
        }

        protected override void Dispose(bool disposing)
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
                base.Dispose(disposing);
            }
        }
    }
}
