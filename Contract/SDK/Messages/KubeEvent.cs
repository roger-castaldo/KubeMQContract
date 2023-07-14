using KubeMQ.Contract.SDK.Interfaces;

namespace KubeMQ.Contract.SDK.Messages
{
    internal class KubeEvent : KubeMessage,IKubeEvent
    {
        public bool Stored { get; init; }

        public KubeEvent(IKubeMessage baseMessage)
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
