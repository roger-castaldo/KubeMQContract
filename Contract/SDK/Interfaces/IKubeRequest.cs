using static KubeMQ.Contract.SDK.Grpc.Request.Types;

namespace KubeMQ.Contract.SDK.Interfaces
{
    internal interface IKubeRequest : IKubeMessage
    {
        int Timeout { get; }
        RequestType CommandType { get; }
    }
}
