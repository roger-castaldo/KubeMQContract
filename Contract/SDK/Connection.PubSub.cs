using Google.Protobuf;
using Grpc.Core;
using KubeMQ.Contract.Interfaces;
using KubeMQ.Contract.Messages;
using KubeMQ.Contract.SDK.Grpc;
using KubeMQ.Contract.Subscriptions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KubeMQ.Contract.SDK
{
    internal partial class Connection : IPubSubConnection
    {

        public async Task<ITransmissionResult> Send<T>(T message, CancellationToken cancellationToken = new CancellationToken(), string? channel = null, Dictionary<string, string>? tagCollection = null)
        {
            try
            {
                var msg = GetMessageFactory<T>().Event(message, connectionOptions, channel, tagCollection);
                Log(LogLevel.Information, "Sending Message {} of type {}", msg.ID, typeof(T).Name);
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

        public Guid Subscribe<T>(Action<Contract.Interfaces.IMessage<T>> messageRecieved, Action<Exception> errorRecieved, CancellationToken cancellationToken = new CancellationToken(), string? channel = null, string group = "", long storageOffset = 0, MessageReadStyle? messageReadStyle = null)
        {
            var sub = new EventSubscription<T>(GetMessageFactory<T>(), new KubeSubscription<T>(this.connectionOptions, channel: channel, group: group), this.client, this.connectionOptions, messageRecieved, errorRecieved, storageOffset, this, messageReadStyle, cancellationToken);
            Log(LogLevel.Information, "Requesting Subscribe {} of type {}", sub.ID, typeof(T).Name);
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            sub.Start();
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            lock (subscriptions)
            {
                subscriptions.Add(sub);
            }
            return sub.ID;
        }

        public IMessageStream<T> SubscribeToStream<T>(Action<Exception> errorRecieved, CancellationToken cancellationToken = default, string? channel = null, string group = "", long storageOffset = 0, MessageReadStyle? messageReadStyle = null)
        {
            var stream = new MessageStream<T>(GetMessageFactory<T>(), new KubeSubscription<T>(this.connectionOptions, channel: channel, group: group), this.client, this.connectionOptions, errorRecieved, storageOffset, this, messageReadStyle, cancellationToken);
            Log(LogLevel.Information, "Requesting MessageStream {} of type {}", stream.ID, typeof(T).Name);
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            stream.Start();
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            lock (subscriptions)
            {
                subscriptions.Add(stream);
            }
            return stream;
        }
    }
}
