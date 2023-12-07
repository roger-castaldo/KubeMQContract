using KubeMQ.Contract.Interfaces.Messages;
using KubeMQ.Contract.SDK.Connection;
using Microsoft.Extensions.Logging;
namespace KubeMQ.Contract.Subscriptions
{
    internal class ReadonlyMessageQueueStream<T> : MessageStream, IReadonlyMessageStream<T>
    {
        public Guid ID { get; private init; }
        private readonly MessageQueue<T> messageQueue;

        public ReadonlyMessageQueueStream(Guid id, IMessageFactory<T> messageFactory, ConnectionOptions connectionOptions, KubeClient client, ILogger? logger, string? channel,CancellationToken cancellationToken) {
            ID = id;
            messageQueue = new MessageQueue<T>(id,messageFactory, connectionOptions, client, logger, channel,cancellationToken);
            messageQueue.CancellationToken.Token.Register(() =>
            {
                base.Dispose(true);
            });
        }

        protected override void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    messageQueue.Dispose();
                }

                base.Dispose(disposing);
            }
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public async IAsyncEnumerator<IMessage<T>> GetAsyncEnumerator(CancellationToken cancellationToken)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            while (!disposedValue && !cancellationToken.IsCancellationRequested && !messageQueue.CancellationToken.IsCancellationRequested)
            {
                IEnumerable<IMessage<T>>? messages;
                try
                {
                    messages = messageQueue.Pop(1, cancellationToken);
                }
                catch (Exception)
                {
                    if(!disposedValue && !cancellationToken.IsCancellationRequested && !messageQueue.CancellationToken.IsCancellationRequested)
                        throw;
                    break;
                }
                if (messages!=null && messages.Any())
                    yield return messages.First();
            }
        }
    }
}
