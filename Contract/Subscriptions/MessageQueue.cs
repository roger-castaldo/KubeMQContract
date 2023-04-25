using KubeMQ.Contract.Attributes;
using KubeMQ.Contract.Interfaces;
using KubeMQ.Contract.SDK.Grpc;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

namespace KubeMQ.Contract.Subscriptions
{
    internal class MessageQueue<T> : IMessageQueue<T>,IDisposable
    {
        private const int PEEK_TIMEOUT_SECONDS = 1;
        private readonly ConnectionOptions connectionOptions;
        private readonly kubemq.kubemqClient client;
        private readonly string channel;
        private readonly CancellationTokenSource cancellationToken;

        public MessageQueue(ConnectionOptions connectionOptions, kubemq.kubemqClient client, string? channel = null)
        {
            this.connectionOptions=connectionOptions;
            this.client=client;
            this.cancellationToken = new CancellationTokenSource();
            this.channel = channel??typeof(T).GetCustomAttributes<MessageChannel>().Select(mc => mc.Name).FirstOrDefault(String.Empty);
            if (string.IsNullOrEmpty(this.channel))
                throw new ArgumentNullException(nameof(channel), "message must have a channel value");
        }

        public bool HasMore
        {
            get
            {
                var res = client.ReceiveQueueMessages(new ReceiveQueueMessagesRequest()
                {
                    RequestID = Guid.NewGuid().ToString(),
                    ClientID = connectionOptions.ClientId,
                    Channel = channel,
                    MaxNumberOfMessages = 1,
                    WaitTimeSeconds = PEEK_TIMEOUT_SECONDS,
                    IsPeak = true
                }, connectionOptions.GrpcMetadata, null, cancellationToken.Token);
                return res!=null && !res.IsError && string.IsNullOrEmpty(res.Error)&&res.MessagesReceived>0;
            }
        }

        public T? Peek()
        {
            var res = client.ReceiveQueueMessages(new ReceiveQueueMessagesRequest()
            {
                RequestID = Guid.NewGuid().ToString(),
                ClientID = connectionOptions.ClientId,
                Channel = channel,
                MaxNumberOfMessages = 1,
                WaitTimeSeconds = PEEK_TIMEOUT_SECONDS,
                IsPeak = true
            }, connectionOptions.GrpcMetadata, null, cancellationToken.Token);
            if (res!=null && !res.IsError && string.IsNullOrEmpty(res.Error)&&res.MessagesReceived>0)
                return Utility.ConvertMessage<T>(res.Messages.First());
            return default(T?);
        }

        public T? Pop()
        {
            var res = client.ReceiveQueueMessages(new ReceiveQueueMessagesRequest()
            {
                RequestID = Guid.NewGuid().ToString(),
                ClientID = connectionOptions.ClientId,
                Channel = channel,
                MaxNumberOfMessages = 1,
                WaitTimeSeconds = PEEK_TIMEOUT_SECONDS,
                IsPeak = false
            }, connectionOptions.GrpcMetadata, null, cancellationToken.Token);
            if (res!=null && !res.IsError && string.IsNullOrEmpty(res.Error)&&res.MessagesReceived>0)
                return Utility.ConvertMessage<T>(res.Messages.First());
            return default(T?);
        }

        public IEnumerable<T> Pop(int count)
        {
            var res = client.ReceiveQueueMessages(new ReceiveQueueMessagesRequest()
            {
                RequestID = Guid.NewGuid().ToString(),
                ClientID = connectionOptions.ClientId,
                Channel = channel,
                MaxNumberOfMessages = count,
                WaitTimeSeconds = PEEK_TIMEOUT_SECONDS,
                IsPeak = false
            }, connectionOptions.GrpcMetadata, null, cancellationToken.Token);
            if (res!=null && !res.IsError && string.IsNullOrEmpty(res.Error)&&res.MessagesReceived>0)
            {
                return res.Messages.Select(msg => Utility.ConvertMessage<T>(msg));
            }
            return Array.Empty<T>();
        }

        public void Dispose()
        {
            cancellationToken.Cancel();
        }
    }
}
