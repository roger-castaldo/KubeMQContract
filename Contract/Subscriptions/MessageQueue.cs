﻿using KubeMQ.Contract.Attributes;
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
        public CancellationTokenSource CancellationToken { get; private init; }
        private readonly ILogger? logger;
        private readonly string clientID;
        private bool disposedValue;

        public Guid ID { get; private init; }

        public MessageQueue(Guid id,IMessageFactory<T> messageFactory,ConnectionOptions connectionOptions, KubeClient client, ILogger? logger, string? channel,CancellationToken cancellationToken)
        {
            ID=id;
            clientID = $"{clientID}_{id}";
            this.messageFactory=messageFactory;
            this.connectionOptions=connectionOptions;
            this.client=client;
            this.logger=logger;
            CancellationToken = new CancellationTokenSource();
            this.channel = channel??typeof(T).GetCustomAttributes<MessageChannel>().Select(mc => mc.Name).FirstOrDefault(string.Empty);
            if (string.IsNullOrEmpty(this.channel))
                throw new ArgumentNullException(nameof(channel), "message must have a channel value");
            logger?.LogTrace("Establishing Message Queue {SubscriptionID} for {MessageType}", ID, Utility.TypeName<T>());

            cancellationToken.Register(() =>
            {
                CancellationToken.Cancel();
            });

            CancellationToken.Token.Register(() =>
            {
                this.Dispose();
            });
        }

        private ReceiveQueueMessagesResponse SubmitRequest(bool peak=true,int count = 1, CancellationToken? cancellationToken=null)
        {
            return client.ReceiveQueueMessages(new ReceiveQueueMessagesRequest()
            {
                RequestID = Guid.NewGuid().ToString(),
                ClientID = clientID,
                Channel = channel,
                MaxNumberOfMessages = count,
                WaitTimeSeconds = PEEK_TIMEOUT_SECONDS,
                IsPeak = peak
            }, connectionOptions.GrpcMetadata, cancellationToken??CancellationToken.Token);
        }

        public bool HasMore
        {
            get
            {
                logger?.LogTrace("Checking if Queue {SubscriptionID} has more", ID);
                var res = SubmitRequest();
                var result = res!=null && !res.IsError && string.IsNullOrEmpty(res.Error)&&res.MessagesReceived>0;
                logger?.LogTrace("Queue {SubscriptionID} HasMore {result}", ID, result);
                return result;
            }
        }

        public IMessage<T>? Peek()
        {
            logger?.LogTrace("Peeking Queue {SubscriptionID}", ID);
            var res = SubmitRequest();
            logger?.LogTrace("Peek results for Queue {SubscriptionID} (IsError:{IsError},Error:{ErrorMessage},MessagesRecieved:{MessagesCount}", ID, res.IsError, res.Error, res.MessagesReceived);
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
            => Pop(count, CancellationToken.Token);

        internal IEnumerable<IMessage<T>> Pop(int count, CancellationToken cancellationToken)
        {
            logger?.LogTrace("Popping Queue {SubscriptionID}, count {count}", ID, count);
            var res = SubmitRequest(peak:false, count:count,cancellationToken:cancellationToken);
            logger?.LogTrace("Peek results for Queue {SubscriptionID} (IsError:{IsError},Error:{ErrorMessage},MessagesRecieved:{MessagesCount}", ID, res.IsError, res.Error, res.MessagesReceived);
            if (res!=null && !res.IsError && string.IsNullOrEmpty(res.Error)&&res.MessagesReceived>0)
                return res.Messages.Select(msg => messageFactory.ConvertMessage(logger,msg));
            return Array.Empty<IMessage<T>>();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (!CancellationToken.IsCancellationRequested)
                        CancellationToken.Cancel();
                    logger?.LogTrace("Disposing of Queue {SubscriptionID}", ID);
                    client.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue=true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~MessageQueue()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
