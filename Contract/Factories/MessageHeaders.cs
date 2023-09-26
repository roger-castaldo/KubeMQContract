using Google.Protobuf.Collections;
using KubeMQ.Contract.Interfaces.Messages;

namespace KubeMQ.Contract.Factories
{
    internal class MessageHeaders : IMessageHeader
    {
        private bool disposedValue;
        private IEnumerable<KeyValuePair<string, string>> Values { get; init; }

        public MessageHeaders(IEnumerable<KeyValuePair<string, string>> values,MapField<string,string>? tags)
        {
            Values = new Dictionary<string, string>()
                .Concat(tags==null ? new Dictionary<string, string>() : tags)
                .Concat(values.Where(pair=>tags==null || !tags.ContainsKey(pair.Key)));
        }

        public IEnumerable<string> Keys => (Values==null ? Array.Empty<string>() : Values.Select(pair=>pair.Key));

        public string? this[string key]
            => Values.FirstOrDefault(pair=>pair.Key.Equals(key, StringComparison.OrdinalIgnoreCase)).Value;
        
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
        // ~MessageHeaders()
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
