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
        private readonly Dictionary<string, string>? tagCollection;
        public Guid ID => Guid.NewGuid();

        private readonly IEnumerable<T> items;
        public IEnumerable<QueueMessage> Messages => items.Select(item =>
        {
            var msg = new KubeEnqueue<T>(item,connectionOptions,channel,delaySeconds,expirationSeconds,maxCount,maxCountChannel,tagCollection);
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

        public KubeBatchEnqueue(IEnumerable<T> items,ConnectionOptions connectionOptions, string? channel, int? delaySeconds, int? expirationSeconds, int? maxCount, string? maxCountChannel, Dictionary<string, string>? tagCollection)
        {
            this.items=items;
            this.connectionOptions = connectionOptions;
            this.channel = channel;
            this.delaySeconds = delaySeconds;
            this.expirationSeconds = expirationSeconds;
            this.maxCount = maxCount;
            this.maxCountChannel = maxCountChannel;
            this.tagCollection=tagCollection;
        }
    }
}
