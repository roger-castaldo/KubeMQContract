using KubeMQ.Contract.SDK.Grpc;

namespace KubeMQ.Contract.SDK.Interfaces
{
    internal interface IKubeBatchEnqueue
    {
        Guid ID { get; }
        IEnumerable<QueueMessage> Messages { get; }
    }
}
