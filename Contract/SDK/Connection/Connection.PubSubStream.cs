using KubeMQ.Contract.Interfaces.Connections;
using KubeMQ.Contract.Interfaces.Messages;
using KubeMQ.Contract.Subscriptions;
using Microsoft.Extensions.Logging;

namespace KubeMQ.Contract.SDK.Connection
{
    internal partial class Connection : IPubSubStreamConnection
    {
        public IReadonlyMessageStream<T> SubscribeToStream<T>(Action<Exception> errorRecieved, string? channel = null, string group = "", long storageOffset = 0, MessageReadStyle? messageReadStyle = null,bool ignoreMessageHeader= false, CancellationToken cancellationToken = default)
        {
            Guid id = Guid.NewGuid();
            var stream =  RegisterSubscription<ReadonlyMessageStream<T>>(
                new ReadonlyMessageStream<T>(
                    id,
                    GetMessageFactory<T>(ignoreMessageHeader), 
                    new KubeSubscription<T>(clientID,id, channel: channel, group: group), 
                    EstablishConnection(), 
                    this.connectionOptions, 
                    errorRecieved, 
                    storageOffset,
                    ProduceLogger(id), 
                    messageReadStyle, 
                    cancellationToken)
            );
            Log(LogLevel.Information, "Registered MessageStream {} of type {}", stream.ID, Utility.TypeName<T>());
            return stream;
        }

        public IWritableMessageStream<T> CreateStream<T>(string? channel = null)
        {
            Log(LogLevel.Information, "Producing a WritableStream of type {}", Utility.TypeName<T>());
            return new WritableMessageStream<T>(this, channel);
        }
    }
}
