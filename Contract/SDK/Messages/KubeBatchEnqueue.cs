using KubeMQ.Contract.SDK.Grpc;
using KubeMQ.Contract.SDK.Interfaces;

namespace KubeMQ.Contract.SDK.Messages
{
    internal class KubeBatchEnqueue : IKubeBatchEnqueue
    {
        public Guid ID => Guid.NewGuid();
        public IEnumerable<QueueMessage> Messages { get; init; } = Array.Empty<QueueMessage>();
    }
}
