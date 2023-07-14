using KubeMQ.Contract.Interfaces.Messages;

namespace KubeMQ.Contract.Messages
{
    internal class ResultMessage<T> : TransmittedMessage, IResultMessage<T>
    {
        public T? Response { get; init; }

        protected override void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                    if (Response!=null && typeof(T).GetInterfaces().Any(t => t==typeof(IDisposable)))
                    {
                        try
                        {
                            ((IDisposable)Response).Dispose();
                        }
                        catch (Exception) { }
                    }
                }
                base.Dispose(disposing);
            }
        }
    }
}
