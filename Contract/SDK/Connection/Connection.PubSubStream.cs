using KubeMQ.Contract.Interfaces;
using KubeMQ.Contract.Interfaces.Connections;
using KubeMQ.Contract.Interfaces.Messages;
using KubeMQ.Contract.Subscriptions;
using Microsoft.Extensions.Logging;

namespace KubeMQ.Contract.SDK.Connection
{
    internal partial class Connection : IPubSubStreamConnection
    {
        public IReadonlyMessageStream<T> SubscribeToStream<T>(Action<Exception> errorRecieved, CancellationToken cancellationToken = default, string? channel = null, string group = "", long storageOffset = 0, MessageReadStyle? messageReadStyle = null)
        {
            var stream = new ReadonlyMessageStream<T>(GetMessageFactory<T>(), new KubeSubscription<T>(this.connectionOptions, channel: channel, group: group), this.client, this.connectionOptions, errorRecieved, storageOffset, this, messageReadStyle, cancellationToken);
            Log(LogLevel.Information, "Requesting MessageStream {} of type {}", stream.ID, Utility.TypeName<T>());
            stream.Start();
            dataLock.EnterWriteLock();
            subscriptions.Add(stream);
            dataLock.ExitWriteLock();
            return stream;
        }

        public IWritableMessageStream<T> CreateStream<T>(string? channel = null)
        {
            var stream = new WritableMessageStream<T>(this, channel);
            Log(LogLevel.Information, "Producing a WritableStream of type {}", Utility.TypeName<T>());
            return stream;
        }
    }
}
