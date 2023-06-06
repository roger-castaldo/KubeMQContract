using Google.Protobuf.Collections;
using KubeMQ.Contract.Interfaces.Messages;

namespace KubeMQ.Contract.Factories
{
    internal class MessageHeaders : IMessageHeader
    {
        public MapField<string, string>? Tags { get; init; }

        public IEnumerable<string> Keys => (Tags==null ? Array.Empty<string>() : Tags.Keys);

        public string? this[string key]
        {
            get
            {
                string? value = null;
                if (Tags==null)
                    return value;
                Tags.TryGetValue(key, out value);
                return value;
            }
        }
    }
}
