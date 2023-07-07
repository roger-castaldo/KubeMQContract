using Google.Protobuf;
using Grpc.Core;
using KubeMQ.Contract.Interfaces;
using KubeMQ.Contract.Interfaces.Connections;
using KubeMQ.Contract.Messages;
using KubeMQ.Contract.SDK.Grpc;
using KubeMQ.Contract.Subscriptions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KubeMQ.Contract.SDK.Connection
{
    internal partial class Connection : IPubSubConnection
    {

        public async Task<ITransmissionResult> Send<T>(T message, string? channel = null, Dictionary<string, string>? tagCollection = null, CancellationToken cancellationToken = new CancellationToken())
        {
            try
            {
                var msg = GetMessageFactory<T>().Event(message, connectionOptions, channel, tagCollection);
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
                }, connectionOptions.GrpcMetadata, null, cancellationToken);
                Log(LogLevel.Information, "Transmission Result for {} (IsError:{},Error:{})", msg.ID, !string.IsNullOrEmpty(res.Error), res.Error);
                return new TransmissionResult()
                {
                    MessageID=new Guid(msg.ID),
                    IsError = !string.IsNullOrEmpty(res.Error),
                    Error=res.Error
                };
            }
            catch (RpcException ex)
            {
                Log(LogLevel.Error, "RPC error occured on Send in send Message:{}, Status: {}", ex.Message, ex.Status);
                return new TransmissionResult()
                {
                    IsError=true,
                    Error=$"Message: {ex.Message}, Status: {ex.Status}"
                };
            }
            catch (Exception ex)
            {
                Log(LogLevel.Error, "Exception occured in Send Message:{}", ex.Message);
                return new TransmissionResult()
                {
                    IsError=true,
                    Error=ex.Message
                };
            }
        }

        public Guid Subscribe<T>(Action<Contract.Interfaces.Messages.IMessage<T>> messageRecieved, Action<Exception> errorRecieved, string? channel = null, string group = "", long storageOffset = 0, MessageReadStyle? messageReadStyle = null, CancellationToken cancellationToken = new CancellationToken())
        {
            var sub = new EventSubscription<T>(GetMessageFactory<T>(), new KubeSubscription<T>(this.connectionOptions, channel: channel, group: group), this.client, this.connectionOptions, messageRecieved, errorRecieved, storageOffset, this, messageReadStyle,false, cancellationToken);
            Log(LogLevel.Information, "Requesting Subscribe {} of type {}", sub.ID, Utility.TypeName<T>());
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            sub.Start();
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            dataLock.EnterWriteLock();
            subscriptions.Add(sub);
            dataLock.ExitWriteLock();
            return sub.ID;
        }
    }
}
