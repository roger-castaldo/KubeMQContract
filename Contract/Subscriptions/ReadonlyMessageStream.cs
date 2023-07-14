using KubeMQ.Contract.Interfaces;
using KubeMQ.Contract.SDK.Grpc;
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
        private readonly EventSubscription<T> _subscription;
        private readonly Channel<IMessage<T>> _channel;

        public ReadonlyMessageStream(Guid id,IMessageFactory<T> messageFactory, KubeSubscription<T> subscription, KubeClient client, ConnectionOptions options, Action<Exception> errorRecieved, long storageOffset, ILogger? logProvider, MessageReadStyle? messageReadStyle, CancellationToken cancellationToken)
        {
            ID=id;
            cancellationToken.Register(() => { Stop(); });
            _channel = Channel.CreateUnbounded<IMessage<T>>(new UnboundedChannelOptions()
            {
                SingleReader=true,
                SingleWriter=true
            });
            _subscription = new EventSubscription<T>(id,messageFactory, subscription, client, options,async (message) => {
                success++;
                await _channel.Writer.WriteAsync(message);
            }, err =>
            {
                errors++;
                errorRecieved(err);
            }, storageOffset, logProvider, messageReadStyle,cancellationToken);
        }

        protected override void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    try
                    {
                        _subscription.Stop();
                    }
                    catch (Exception) { }
                    try
                    {
                        _channel.Writer.Complete();
                    }
                    catch (Exception) { }
                    base.Dispose(disposing);
                    if (_channel.Reader.TryPeek(out _))
                        throw new ArgumentOutOfRangeException("Items", "There are unprocessed items in the stream.");
                }
            }
        }

        public IAsyncEnumerator<IMessage<T>> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            return _channel.Reader.ReadAllAsync(cancellationToken).GetAsyncEnumerator(cancellationToken);
        }

        public void Start()
        {
            _subscription.Start();
        }

        public void Stop()
        {
            Dispose();
        }
    }
}
