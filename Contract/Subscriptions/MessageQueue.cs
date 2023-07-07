using KubeMQ.Contract.Attributes;
using KubeMQ.Contract.Interfaces;
using KubeMQ.Contract.Interfaces.Messages;
using KubeMQ.Contract.SDK.Grpc;
using System.Reflection;

namespace KubeMQ.Contract.Subscriptions
{
    internal class MessageQueue<T> : IMessageQueue<T>,IDisposable
    {
        private const int PEEK_TIMEOUT_SECONDS = 1;
        private const int POP_TIMEOUT_SECONDS = PEEK_TIMEOUT_SECONDS * 5;

        private readonly IMessageFactory<T> messageFactory;
        private readonly ConnectionOptions connectionOptions;
        private readonly kubemq.kubemqClient client;
        private readonly string channel;
        private readonly CancellationTokenSource cancellationToken;
        private readonly ILogProvider logProvider;

        public Guid ID => Guid.NewGuid();

        public MessageQueue(IMessageFactory<T> messageFactory,ConnectionOptions connectionOptions, kubemq.kubemqClient client,ILogProvider logProvider, string? channel)
        {
            this.messageFactory=messageFactory;
            this.connectionOptions=connectionOptions;
            this.client=client;
            this.logProvider = logProvider;
            this.cancellationToken = new CancellationTokenSource();
            this.channel = channel??typeof(T).GetCustomAttributes<MessageChannel>().Select(mc => mc.Name).FirstOrDefault(String.Empty);
            if (string.IsNullOrEmpty(this.channel))
                throw new ArgumentNullException(nameof(channel), "message must have a channel value");
            logProvider.LogTrace("Establishing Message Queue {} for {}", ID, Utility.TypeName<T>());
        }

        public bool HasMore
        {
            get
            {
                logProvider.LogTrace("Checking if Queue {} has more", ID);
                var res = client.ReceiveQueueMessages(new ReceiveQueueMessagesRequest()
                {
                    RequestID = Guid.NewGuid().ToString(),
                    ClientID = connectionOptions.ClientId,
                    Channel = channel,
                    MaxNumberOfMessages = 1,
                    WaitTimeSeconds = PEEK_TIMEOUT_SECONDS,
                    IsPeak = true
                }, connectionOptions.GrpcMetadata, null, cancellationToken.Token);
                var result = res!=null && !res.IsError && string.IsNullOrEmpty(res.Error)&&res.MessagesReceived>0;
                logProvider.LogTrace("Queue {} HasMore result {}", ID, result);
                return result;
            }
        }

        public IMessage<T>? Peek()
        {
            logProvider.LogTrace("Peeking Queue {}", ID);
            var res = client.ReceiveQueueMessages(new ReceiveQueueMessagesRequest()
            {
                RequestID = Guid.NewGuid().ToString(),
                ClientID = connectionOptions.ClientId,
                Channel = channel,
                MaxNumberOfMessages = 1,
                WaitTimeSeconds = POP_TIMEOUT_SECONDS,
                IsPeak = true
            }, connectionOptions.GrpcMetadata, null, cancellationToken.Token);
            logProvider.LogTrace("Peek results for Queue {} (IsError:{},Error:{},MessagesRecieved:{}",ID,res.IsError,res.Error,res.MessagesReceived);
            if (res!=null && !res.IsError && string.IsNullOrEmpty(res.Error)&&res.MessagesReceived>0)
                return messageFactory.ConvertMessage(logProvider,res.Messages.First());
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
            logProvider.LogTrace("Popping Queue {}, count {}", ID,count);
            var res = client.ReceiveQueueMessages(new ReceiveQueueMessagesRequest()
            {
                RequestID = Guid.NewGuid().ToString(),
                ClientID = connectionOptions.ClientId,
                Channel = channel,
                MaxNumberOfMessages = count,
                WaitTimeSeconds = POP_TIMEOUT_SECONDS,
                IsPeak = false
            }, connectionOptions.GrpcMetadata, null, cancellationToken.Token);
            logProvider.LogTrace("Pop results for Queue {} (IsError:{},Error:{},MessagesRecieved:{}", ID, res.IsError, res.Error, res.MessagesReceived);
            if (res!=null && !res.IsError && string.IsNullOrEmpty(res.Error)&&res.MessagesReceived>0)
                return res.Messages.Select(msg => messageFactory.ConvertMessage(logProvider,msg));
            return Array.Empty<IMessage<T>>();
        }

        public void Dispose()
        {
            logProvider.LogTrace("Disposing of Queue {}", ID);
            cancellationToken.Cancel();
        }
    }
}
