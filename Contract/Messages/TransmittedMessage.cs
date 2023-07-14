using Google.Protobuf.Collections;
using KubeMQ.Contract.Interfaces.Messages;

namespace KubeMQ.Contract.Messages
{
    internal class TransmittedMessage : TransmissionResult, IMessageHeader
    {
        private MapField<string, string>? _tags;
        internal MapField<string, string>? Tags { init { _tags=value; } }

        public IEnumerable<string> Keys => (_tags==null ? Array.Empty<string>() : _tags.Keys);

        public string? this[string key]
        {
            get
            {
                string? value = null;
                _tags?.TryGetValue(key, out value);
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
                    _tags=null;
                }
                base.Dispose(disposing);
            }
        }
    }
}
