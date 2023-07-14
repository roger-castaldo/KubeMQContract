using KubeMQ.Contract.SDK.Interfaces;
using static KubeMQ.Contract.SDK.Grpc.Request.Types;

namespace KubeMQ.Contract.SDK.Messages
{
    internal class KubeRequest : KubeMessage,IKubeRequest
    {
        public int Timeout { get; init; }
        public RequestType CommandType { get; init; }
        public KubeRequest(IKubeMessage baseMessage)
        {
            ID=baseMessage.ID;
            MetaData=baseMessage.MetaData;
            Channel=baseMessage.Channel;
            ClientID=baseMessage.ClientID;
            Body=baseMessage.Body;
            Tags = baseMessage.Tags;
        }
    }
}
