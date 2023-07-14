using KubeMQ.Contract.SDK.Grpc;
using KubeMQ.Contract.SDK.Interfaces;

namespace KubeMQ.Contract.SDK.Messages
{
    internal class KubeEnqueue : KubeMessage, IKubeEnqueue
    {
        public int? DelaySeconds { get; init; }
        public int? ExpirationSeconds { get; init; }
        public int? MaxSize { get; init; }
        public string? MaxCountChannel { get; init; }

        public QueueMessagePolicy Policy
        {
            get
            {
                var result = new QueueMessagePolicy();
                if (DelaySeconds!=null)
                    result.DelaySeconds=DelaySeconds.Value;
                if (ExpirationSeconds!=null)
                    result.ExpirationSeconds=ExpirationSeconds.Value;
                if (MaxSize!=null)
                    result.MaxReceiveCount=MaxSize.Value;
                if (MaxCountChannel!=null)
                    result.MaxReceiveQueue=MaxCountChannel!;
                return result;
            }
        }

        public QueueMessageAttributes Attributes { 
            get
            {
                var result = new QueueMessageAttributes();
                if (DelaySeconds!=null)
                    result.DelayedTo = Utility.ToUnixTime(DateTime.Now.AddSeconds(DelaySeconds.Value));
                if (ExpirationSeconds!=null)
                    result.ExpirationAt = Utility.ToUnixTime(DateTime.Now.AddSeconds(ExpirationSeconds.Value));
                if (MaxSize!=null)
                    result.ReceiveCount = MaxSize.Value;
                return result;
            }
        }

        public KubeEnqueue(IKubeMessage baseMessage)
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
