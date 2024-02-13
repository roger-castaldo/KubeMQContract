using KubeMQ.Contract.SDK;
using System.Threading.Channels;
using KubeMQ.Contract.Interfaces.Messages;
using KubeMQ.Contract.SDK.Connection;
using Microsoft.Extensions.Logging;

namespace KubeMQ.Contract.Subscriptions
{
    internal class ReadonlyMessageStream<T> : MessageStream,IReadonlyMessageStream<T>, IMessageSubscription 
    {
        public Guid ID { get; private init; } = Guid.NewGuid();
        private readonly EventSubscription<T> subscription;
        private readonly Channel<IMessage<T>> channel;

        public ReadonlyMessageStream(Guid id,IMessageFactory<T> messageFactory, KubeSubscription<T> subscription, KubeClient client, ConnectionOptions options, Action<Exception> errorRecieved, long storageOffset, ILogger? logger, MessageReadStyle? messageReadStyle, CancellationToken cancellationToken)
        {
            ID=id;
            cancellationToken.Register(() => { Stop(); });
            channel = Channel.CreateUnbounded<IMessage<T>>(new UnboundedChannelOptions()
            {
                SingleReader=true,
                SingleWriter=true
            });
            this.subscription = new EventSubscription<T>(id, messageFactory, subscription, client, options,async (message) => {
                success++;
                await channel.Writer.WriteAsync(message);
            }, err =>
            {
                errors++;
                errorRecieved(err);
            }, messageReadStyle, storageOffset, logger, true, cancellationToken);
        }

        protected override void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    try
                    {
                        subscription.Stop();
                    }
                    catch (Exception) { }
                    try
                    {
                        channel.Writer.Complete();
                    }
                    catch (Exception) { }
                    base.Dispose(disposing);
                    if (channel.Reader.TryPeek(out _))
                        throw new ArgumentOutOfRangeException("Items", "There are unprocessed items in the stream.");
                }
            }
        }

        public IAsyncEnumerator<IMessage<T>> GetAsyncEnumerator(CancellationToken cancellationToken = default)
            =>channel.Reader.ReadAllAsync(cancellationToken).GetAsyncEnumerator(cancellationToken);

        public void Start()
            =>subscription.Start();

        public void Stop()
            =>Dispose();
    }
}
