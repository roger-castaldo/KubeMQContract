using KubeMQ.Contract.Interfaces;
using KubeMQ.Contract.SDK.Grpc;
using KubeMQ.Contract.SDK;
using System.Threading.Channels;

namespace KubeMQ.Contract.Subscriptions
{
    internal class ReadonlyMessageStream<T> : IReadonlyMessageStream<T>, IMessageSubscription
    {
        public Guid ID => Guid.NewGuid();
        private readonly EventSubscription<T> _subscription;
        private readonly Channel<IMessage<T>> _channel;

        public ReadonlyMessageStream(IMessageFactory<T> messageFactory, KubeSubscription<T> subscription, kubemq.kubemqClient client, ConnectionOptions options, Action<Exception> errorRecieved, long storageOffset, ILogProvider logProvider, MessageReadStyle? messageReadStyle, CancellationToken cancellationToken)
        {
            _channel = Channel.CreateUnbounded<IMessage<T>>(new UnboundedChannelOptions()
            {
                SingleReader=true,
                SingleWriter=true
            });
            _subscription = new EventSubscription<T>(messageFactory, subscription, client, options, (message) => {
                _channel.Writer.WriteAsync(message);
            }, errorRecieved, storageOffset, logProvider, messageReadStyle, cancellationToken);
        }

        public void Dispose()
        {
            try
            {
                _subscription.Stop();
            }
            catch (Exception) { }
            try
            {
                _channel.Writer.Complete();
            } catch (Exception) { }
            if (_channel.Reader.TryPeek(out IMessage<T> item))
                throw new ArgumentOutOfRangeException("Items", "There are unprocessed items in the stream.");
        }

        public IAsyncEnumerator<IMessage<T>> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            return _channel.Reader.ReadAllAsync(cancellationToken).GetAsyncEnumerator(cancellationToken);
        }

        public Task Start()
        {
            return _subscription.Start();
        }

        public void Stop()
        {
            Dispose();
        }
    }
}
