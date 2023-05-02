using Google.Protobuf;
using KubeMQ.Contract.SDK.Grpc;
using KubeMQ.Contract.SDK.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KubeMQ.Contract.SDK.Messages
{
    internal class KubeBatchEnqueue : IKubeBatchEnqueue
    {
        public Guid ID => Guid.NewGuid();
        public IEnumerable<QueueMessage> Messages { get; init; } = Array.Empty<QueueMessage>();
    }
}
