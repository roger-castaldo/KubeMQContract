using KubeMQ.Contract.Attributes;
using KubeMQ.Contract.Interfaces;
using KubeMQ.Contract.Interfaces.Messages;
using KubeMQ.Contract.SDK.Connection;
using KubeMQ.Contract.SDK.Grpc;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace KubeMQ.Contract.Subscriptions
{
    internal class MessageQueue<T> : IMessageQueue<T>,IDisposable 
    {
        private const int PEEK_TIMEOUT_SECONDS = 1;
        private const int POP_TIMEOUT_SECONDS = PEEK_TIMEOUT_SECONDS * 5;

        private readonly IMessageFactory<T> messageFactory;
        private readonly ConnectionOptions connectionOptions;
        private readonly KubeClient client;
        private readonly string channel;
        private readonly CancellationTokenSource cancellationToken;
        private readonly ILogger? logger;
        private readonly string clientID;

        public Guid ID { get; private init; }

        public MessageQueue(Guid id,IMessageFactory<T> messageFactory,ConnectionOptions connectionOptions, KubeClient client, ILogger? logger, string? channel)
        {
            ID=id;
            clientID = $"{clientID}_{id}";
            this.messageFactory=messageFactory;
            this.connectionOptions=connectionOptions;
            this.client=client;
            this.logger=logger;
            this.cancellationToken = new CancellationTokenSource();
            this.channel = channel??typeof(T).GetCustomAttributes<MessageChannel>().Select(mc => mc.Name).FirstOrDefault(String.Empty);
            if (string.IsNullOrEmpty(this.channel))
                throw new ArgumentNullException(nameof(channel), "message must have a channel value");
            logger?.LogTrace("Establishing Message Queue {} for {}", ID, Utility.TypeName<T>());

            cancellationToken.Token.Register(() =>
            {
                client.Dispose();
            });
        }

        public bool HasMore
        {
            get
            {
                logger?.LogTrace("Checking if Queue {} has more", ID);
                var res = client.ReceiveQueueMessages(new ReceiveQueueMessagesRequest()
                {
                    RequestID = Guid.NewGuid().ToString(),
                    ClientID = clientID,
                    Channel = channel,
                    MaxNumberOfMessages = 1,
                    WaitTimeSeconds = PEEK_TIMEOUT_SECONDS,
                    IsPeak = true
                }, connectionOptions.GrpcMetadata, cancellationToken.Token);
                var result = res!=null && !res.IsError && string.IsNullOrEmpty(res.Error)&&res.MessagesReceived>0;
                logger?.LogTrace("Queue {} HasMore result {}", ID, result);
                return result;
            }
        }

        public IMessage<T>? Peek()
        {
            logger?.LogTrace("Peeking Queue {}", ID);
            var res = client.ReceiveQueueMessages(new ReceiveQueueMessagesRequest()
            {
                RequestID = Guid.NewGuid().ToString(),
                ClientID = clientID,
                Channel = channel,
                MaxNumberOfMessages = 1,
                WaitTimeSeconds = POP_TIMEOUT_SECONDS,
                IsPeak = true
            }, connectionOptions.GrpcMetadata, cancellationToken.Token);
            logger?.LogTrace("Peek results for Queue {} (IsError:{},Error:{},MessagesRecieved:{}", ID, res.IsError, res.Error, res.MessagesReceived);
            if (res!=null && !res.IsError && string.IsNullOrEmpty(res.Error)&&res.MessagesReceived>0)
                return messageFactory.ConvertMessage(logger,res.Messages.First());
            return default;
        }

        public IMessage<T>? Pop()
        {
            var data = Pop(1);
            if (data.Any())
                return data.First();
            return default;
        }

        public IEnumerable<IMessage<T>> Pop(int count)
        {
            logger?.LogTrace("Popping Queue {}, count {}", ID, count);
            var res = client.ReceiveQueueMessages(new ReceiveQueueMessagesRequest()
            {
                RequestID = Guid.NewGuid().ToString(),
                ClientID = clientID,
                Channel = channel,
                MaxNumberOfMessages = count,
                WaitTimeSeconds = POP_TIMEOUT_SECONDS,
                IsPeak = false
            }, connectionOptions.GrpcMetadata, cancellationToken.Token);
            logger?.LogTrace("Pop results for Queue {} (IsError:{},Error:{},MessagesRecieved:{}", ID, res.IsError, res.Error, res.MessagesReceived);
            if (res!=null && !res.IsError && string.IsNullOrEmpty(res.Error)&&res.MessagesReceived>0)
                return res.Messages.Select(msg => messageFactory.ConvertMessage(logger,msg));
            return Array.Empty<IMessage<T>>();
        }

        public void Dispose()
        {
            logger?.LogTrace("Disposing of Queue {}", ID);
            cancellationToken.Cancel();
        }
    }
}
