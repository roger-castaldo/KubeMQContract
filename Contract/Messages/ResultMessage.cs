using Google.Protobuf.Collections;
using KubeMQ.Contract.Interfaces.Messages;

namespace KubeMQ.Contract.Messages
{
    internal class ResultMessage<T> : TransmissionResult, IResultMessage<T>
    {
        public T? Response { get; private init; }
        public IMessageHeader Headers { get; private init; }

        public ResultMessage(Guid? id = null, string? error = null, MapField<string,string>? tags=null,T? response=default(T?))
            : base(id, error)
        {
            Response=response;
            Headers = new ReadonlyMessageHeader(tags);
        }

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
