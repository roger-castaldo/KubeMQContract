using Google.Protobuf;
using Grpc.Core;
using KubeMQ.Contract.Interfaces;
using KubeMQ.Contract.Interfaces.Connections;
using KubeMQ.Contract.Messages;
using KubeMQ.Contract.SDK.Grpc;
using KubeMQ.Contract.Subscriptions;
using Microsoft.Extensions.Logging;

namespace KubeMQ.Contract.SDK.Connection
{
    internal partial class Connection : IPubSubConnection
    {

        public async Task<ITransmissionResult> Send<T>(T message, string? channel = null, Dictionary<string, string>? tagCollection = null, CancellationToken cancellationToken = new CancellationToken())
        {
            try
            {
                var msg = GetMessageFactory<T>().Event(message, connectionOptions,clientID, channel, tagCollection);
                Log(LogLevel.Information, "Sending Message {} of type {}", msg.ID, Utility.TypeName<T>());
                var res = await client.SendEventAsync(new Event
                {
                    EventID = msg.ID,
                    ClientID = msg.ClientID,
                    Channel = msg.Channel,
                    Metadata = msg.MetaData,
                    Body = ByteString.CopyFrom(msg.Body),
                    Store = msg.Stored,
                    Tags = { msg.Tags }
                }, connectionOptions.GrpcMetadata, cancellationToken);
                Log(LogLevel.Information, "Transmission Result for {} (IsError:{},Error:{})", msg.ID, !string.IsNullOrEmpty(res.Error), res.Error);
                return new TransmissionResult(id:new Guid(msg.ID),res.Error);
            }
            catch (RpcException ex)
            {
                Log(LogLevel.Error, "RPC error occured on Send in send Message:{}, Status: {}", ex.Message, ex.Status);
                return new TransmissionResult(error:$"Status: {ex.Status}, Message: {ex.Message}");
            }
            catch (Exception ex)
            {
                Log(LogLevel.Error, "Exception occured in Send Message:{}", ex.Message);
                return new TransmissionResult(error: ex.Message);
            }
        }

        public Guid Subscribe<T>(Action<KubeMQ.Contract.Interfaces.Messages.IMessage<T>> messageRecieved, Action<Exception> errorRecieved, string? channel = null, string group = "", long storageOffset = 0, MessageReadStyle? messageReadStyle = null,bool ignoreMessageHeader=false, CancellationToken cancellationToken = new CancellationToken())
            => CreateSubscription<T>(messageRecieved, errorRecieved, channel, group, storageOffset, messageReadStyle, true,ignoreMessageHeader, cancellationToken);

        public Guid SubscribeAsync<T>(Action<KubeMQ.Contract.Interfaces.Messages.IMessage<T>> messageRecieved, Action<Exception> errorRecieved, string? channel = null, string group = "", long storageOffset = 0, MessageReadStyle? messageReadStyle = null, bool ignoreMessageHeader = false, CancellationToken cancellationToken = new CancellationToken())
            => CreateSubscription<T>(messageRecieved,errorRecieved,channel,group,storageOffset,messageReadStyle,false,ignoreMessageHeader,cancellationToken);

        private Guid CreateSubscription<T>(
            Action<KubeMQ.Contract.Interfaces.Messages.IMessage<T>> messageRecieved,
            Action<Exception> errorRecieved,
            string? channel,
            string group,
            long storageOffset,
            MessageReadStyle? messageReadStyle,
            bool synchronous,
            bool ignoreMessageHeader,
            CancellationToken cancellationToken)
        {
            var id = Guid.NewGuid();
            var sub = RegisterSubscription<EventSubscription<T>>(
                new EventSubscription<T>(
                    id,
                    GetMessageFactory<T>(ignoreMessageHeader),
                    new KubeSubscription<T>(clientID, id, channel: channel, group: group),
                    EstablishConnection(),
                    this.connectionOptions,
                    new Func<KubeMQ.Contract.Interfaces.Messages.IMessage<T>, Task>(
                        msg => {
                            try
                            {
                                messageRecieved(msg);
                                return Task.CompletedTask;
                            }
                            catch (Exception) { throw; }
                        }
                    ),
                    errorRecieved,
                    messageReadStyle,
                    storageOffset,
                    ProduceLogger(id),
                    synchronous,
                    cancellationToken)
            );
            Log(LogLevel.Information, "Registered Subscribe {} of type {}", sub.ID, Utility.TypeName<T>());
            return sub.ID;
        }
    }
}
