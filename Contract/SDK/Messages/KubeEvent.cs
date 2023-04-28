using KubeMQ.Contract.Attributes;
using KubeMQ.Contract.SDK.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace KubeMQ.Contract.SDK.Messages
{
    internal class KubeEvent<T> : KubeMessage<T>,IKubeEvent
    {
        public bool Stored => typeof(T).GetCustomAttributes<StoredMessage>().FirstOrDefault() != null;

        public KubeEvent(T message, ConnectionOptions connectionOptions, string? channel, Dictionary<string, string>? tagCollection)
            :base(message,connectionOptions,channel,tagCollection) { }
    }
}
