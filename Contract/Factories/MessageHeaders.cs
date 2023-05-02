using Google.Protobuf.Collections;
using KubeMQ.Contract.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
                if (Tags!=null)
                    Tags.TryGetValue(key, out value);
                return value;
            }
        }
    }
}
