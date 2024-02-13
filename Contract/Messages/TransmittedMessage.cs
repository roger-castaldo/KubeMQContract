using Google.Protobuf.Collections;
using KubeMQ.Contract.Interfaces.Messages;

namespace KubeMQ.Contract.Messages
{
    internal class TransmittedMessage : TransmissionResult, IMessageHeader
    {
        internal MapField<string, string>? Tags { get; init; }

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

        protected override void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)

                }
                base.Dispose(disposing);
            }
        }
    }
}
