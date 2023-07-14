using KubeMQ.Contract.SDK.Grpc;

namespace KubeMQ.Contract.SDK.Interfaces
{
    internal interface IKubeEnqueue : IKubeMessage
    {
        int? DelaySeconds { get; }
        int? ExpirationSeconds { get; }
        int? MaxSize { get; }
        string? MaxCountChannel { get; }
        QueueMessagePolicy Policy { get; }
        QueueMessageAttributes Attributes { get; }
    }
}
