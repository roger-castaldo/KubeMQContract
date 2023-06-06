using KubeMQ.Contract.Interfaces;
using KubeMQ.Contract.SDK.Grpc;
using KubeMQ.Contract.SDK;
using System.Threading.Channels;
using KubeMQ.Contract.Interfaces.Messages;

namespace KubeMQ.Contract.Subscriptions
{
    internal class ReadonlyMessageStream<T> : MessageStream,IReadonlyMessageStream<T>, IMessageSubscription
    {
        public Guid ID => Guid.NewGuid();
        private readonly EventSubscription<T> _subscription;
        private readonly Channel<IMessage<T>> _channel;

        public ReadonlyMessageStream(IMessageFactory<T> messageFactory, KubeSubscription<T> subscription, kubemq.kubemqClient client, ConnectionOptions options, Action<Exception> errorRecieved, long storageOffset, ILogProvider logProvider, MessageReadStyle? messageReadStyle, CancellationToken cancellationToken)
        {
            cancellationToken.Register(() => { Stop(); });
            _channel = Channel.CreateUnbounded<IMessage<T>>(new UnboundedChannelOptions()
            {
                SingleReader=true,
                SingleWriter=true
            });
            _subscription = new EventSubscription<T>(messageFactory, subscription, client, options,async (message) => {
                success++;
                await _channel.Writer.WriteAsync(message);
            }, err =>
            {
                errors++;
                errorRecieved(err);
            }, storageOffset, logProvider, messageReadStyle, cancellationToken);
        }

        public override void Dispose()
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
            base.Dispose();
            if (_channel.Reader.TryPeek(out _))
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
