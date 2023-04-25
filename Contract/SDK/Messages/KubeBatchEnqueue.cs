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
    internal class KubeBatchEnqueue<T> : IKubeBatchEnqueue
    {
        private readonly ConnectionOptions connectionOptions;
        private readonly string? channel;
        private readonly int? delaySeconds;
        private readonly int? expirationSeconds;
        private readonly int? maxCount;
        private readonly string? maxCountChannel;
        public Guid ID => Guid.NewGuid();

        private readonly IEnumerable<T> items;
        public IEnumerable<QueueMessage> Messages => items.Select(item =>
        {
            var msg = new KubeEnqueue<T>(item,connectionOptions,channel:channel,delaySeconds:delaySeconds,expirationSeconds:expirationSeconds,maxCount:maxCount,maxCountChannel:maxCountChannel);
            return new QueueMessage()
            {
                MessageID= msg.ID,
                ClientID = msg.ClientID,
                Channel = msg.Channel,
                Metadata = msg.MetaData,
                Body = ByteString.CopyFrom(msg.Body),
                Tags = { msg.Tags },
                Policy = msg.Policy
            };
        });

        public KubeBatchEnqueue(IEnumerable<T> items,ConnectionOptions connectionOptions, string? channel = null, int? delaySeconds = null, int? expirationSeconds = null, int? maxCount = null, string? maxCountChannel = null)
        {
            this.items=items;
            this.connectionOptions = connectionOptions;
            this.channel = channel;
            this.delaySeconds = delaySeconds;
            this.expirationSeconds = expirationSeconds;
            this.maxCount = maxCount;
            this.maxCountChannel = maxCountChannel;
        }
    }
}
