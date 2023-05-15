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
    internal partial class Connection : IQueueConnection
    {
        public async Task<ITransmissionResult> EnqueueMessage<T>(T message, CancellationToken cancellationToken = new CancellationToken(), string? channel = null, Dictionary<string, string>? tagCollection = null, int? expirationSeconds = null, int? delaySeconds = null, int? maxQueueSize = null, string? maxQueueChannel = null)
        {
            try
            {
                var msg = GetMessageFactory<T>().Enqueue(message, connectionOptions, channel, tagCollection, delaySeconds, expirationSeconds, maxQueueSize, maxQueueChannel);
                Log(LogLevel.Information, "Sending EnqueueMessage {} of type {}", msg.ID, typeof(T).Name);
                var res = await client.SendQueueMessageAsync(new QueueMessage()
                {
                    MessageID= msg.ID,
                    ClientID = msg.ClientID,
                    Channel = msg.Channel,
                    Metadata = msg.MetaData,
                    Body = ByteString.CopyFrom(msg.Body),
                    Tags = { msg.Tags },
                    Policy = msg.Policy,
                    Attributes = msg.Attributes
                }, connectionOptions.GrpcMetadata, null, cancellationToken);
                Log(LogLevel.Information, "Transmission Result for EnqueueMessage {} (IsError:{},Error:{})", msg.ID, !string.IsNullOrEmpty(res.Error), res.Error);
                return new TransmissionResult()
                {
                    MessageID=new Guid(msg.ID),
                    IsError = !string.IsNullOrEmpty(res.Error),
                    Error=res.Error
                };
            }
            catch (RpcException ex)
            {
                Log(LogLevel.Error, "RPC error occured on EnqueueMessage in send Message:{}, Status: {}", ex.Message, ex.Status);
                return new TransmissionResult()
                {
                    IsError=true,
                    Error=$"Message: {ex.Message}, Status: {ex.Status}"
                };
            }
            catch (Exception ex)
            {
                Log(LogLevel.Error, "Exception occured in EnqueueMessage Message:{}, Status: {}", ex.Message);
                return new TransmissionResult()
                {
                    IsError=true,
                    Error=ex.Message
                };
            }
        }

        public async Task<IBatchTransmissionResult> EnqueueMessages<T>(IEnumerable<T> messages, CancellationToken cancellationToken = new CancellationToken(), string? channel = null, Dictionary<string, string>? tagCollection = null, int? expirationSeconds = null, int? delaySeconds = null, int? maxQueueSize = null, string? maxQueueChannel = null)
        {
            try
            {
                var msg = GetMessageFactory<T>().Enqueue(messages, connectionOptions, channel, tagCollection, delaySeconds, expirationSeconds, maxQueueSize, maxQueueChannel);
                Log(LogLevel.Information, "Sending EnqueueMessages {} of type {}", msg.ID, typeof(T).Name);
                var res = await client.SendQueueMessagesBatchAsync(
                    new QueueMessagesBatchRequest()
                    {
                        BatchID=msg.ID.ToString(),
                        Messages={ msg.Messages }
                    }, connectionOptions.GrpcMetadata, null, cancellationToken);
                if (res==null)
                {
                    Log(LogLevel.Error, "EnqueueMessages response for {} is null from KubeMQ server", msg.ID);
                    return new BatchTransmissionResult()
                    {
                        MessageID=msg.ID,
                        Results=new ITransmissionResult[]
                        {
                            new TransmissionResult()
                            {
                                IsError=true,
                                Error="null response recieved from KubeMQ host"
                            }
                        }
                    };
                }
                Log(LogLevel.Information, "Transmission Result for EnqueueMessages {} (Count:{})", msg.ID, res.Results.Count);
                return new BatchTransmissionResult()
                {
                    MessageID=msg.ID,
                    Results=res.Results.AsEnumerable<SendQueueMessageResult>().Select(sqmr =>
                    {
                        return new TransmissionResult()
                        {
                            MessageID=new Guid(sqmr.MessageID),
                            IsError = !string.IsNullOrEmpty(sqmr.Error),
                            Error=sqmr.Error
                        };
                    })
                };
            }
            catch (RpcException ex)
            {
                Log(LogLevel.Error, "RPC error occured on EnqueueMessages in send Message:{}, Status: {}", ex.Message, ex.Status);
                return new BatchTransmissionResult()
                {
                    IsError=true,
                    Error=$"Message: {ex.Message}, Status: {ex.Status}"
                };
            }
            catch (Exception ex)
            {
                Log(LogLevel.Error, "Exception occured in EnqueueMessages Message:{}, Status: {}", ex.Message);
                return new BatchTransmissionResult()
                {
                    IsError=true,
                    Error=ex.Message
                };
            }
        }

        public IMessageQueue<T> SubscribeToQueue<T>(CancellationToken cancellationToken = default, string? channel = null)
        {
            Log(LogLevel.Information, "Requesting SubscribeToQueue of type {}", typeof(T).Name);
            return new MessageQueue<T>(GetMessageFactory<T>(), connectionOptions, client, this, channel);
        }

    }
}
