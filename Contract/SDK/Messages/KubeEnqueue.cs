using KubeMQ.Contract.Attributes;
using KubeMQ.Contract.SDK.Grpc;
using KubeMQ.Contract.SDK.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace KubeMQ.Contract.SDK.Messages
{
    internal class KubeEnqueue<T> : KubeMessage<T>, IKubeEnqueue
    {
        private readonly int? delaySeconds;
        public int? DelaySeconds => delaySeconds;

        private readonly int? expirationSeconds;
        public int? ExpirationSeconds => expirationSeconds;

        private readonly int? maxCount;
        public int? MaxSize => maxCount;

        private readonly string? maxCountChannel;
        public string? MaxCountChannel => maxCountChannel;

        public QueueMessagePolicy Policy
        {
            get
            {
                var result = new QueueMessagePolicy();
                if (delaySeconds!=null)
                    result.DelaySeconds=delaySeconds.Value;
                if (expirationSeconds!=null)
                    result.ExpirationSeconds=expirationSeconds.Value;
                if (maxCount!=null)
                    result.MaxReceiveCount=maxCount.Value;
                if (maxCountChannel!=null)
                    result.MaxReceiveQueue=maxCountChannel!;
                return result;
            }
        }

        public QueueMessageAttributes Attributes { 
            get
            {
                var result = new QueueMessageAttributes();
                if (delaySeconds!=null)
                    result.DelayedTo = Utility.ToUnixTime(DateTime.Now.AddSeconds(delaySeconds.Value));
                if (expirationSeconds!=null)
                    result.ExpirationAt = Utility.ToUnixTime(DateTime.Now.AddSeconds(expirationSeconds.Value));
                if (maxCount!=null)
                    result.ReceiveCount = maxCount.Value;
                return result;
            }
        }

        public KubeEnqueue(T message, ConnectionOptions connectionOptions, string? channel = null,int? delaySeconds=null, int? expirationSeconds = null, int? maxCount = null, string? maxCountChannel = null) 
            : base(message, connectionOptions, channel)
        {
            var policy = typeof(T).GetCustomAttributes<MessageQueuePolicy>().FirstOrDefault();
            this.delaySeconds = delaySeconds;
            this.expirationSeconds = expirationSeconds??(policy==null ? null : policy.ExpirationSeconds);
            this.maxCount = maxCount??(policy==null ? null : policy.MaxCount);
            this.maxCountChannel=maxCountChannel??(policy==null ? null : policy.MaxCountChannel);

            if ((this.maxCount!=null && this.maxCountChannel==null)
                ||(this.maxCount==null&&this.maxCountChannel!=null))
                throw new ArgumentOutOfRangeException("You must specify both the maxRecieveCount and maxRecieveQueue if you are specifying either");

        }

        
    }
}
