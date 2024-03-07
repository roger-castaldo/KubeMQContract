using Google.Protobuf;
using Grpc.Core;
using KubeMQ.Contract.Interfaces;
using KubeMQ.Contract.Interfaces.Connections;
using KubeMQ.Contract.Interfaces.Messages;
using KubeMQ.Contract.Messages;
using KubeMQ.Contract.SDK.Grpc;
using KubeMQ.Contract.Subscriptions;
using Microsoft.Extensions.Logging;

namespace KubeMQ.Contract.SDK.Connection
{
    internal partial class Connection : IQueueConnection
    {
        public async Task<ITransmissionResult> EnqueueMessage<T>(T message, string? channel = null, Dictionary<string, string>? tagCollection = null, int? expirationSeconds = null, int? delaySeconds = null, int? maxQueueSize = null, string? maxQueueChannel = null, CancellationToken cancellationToken = new CancellationToken())
        {
            try
            {
                var msg = GetMessageFactory<T>().Enqueue(message, connectionOptions,clientID, channel, tagCollection, delaySeconds, expirationSeconds, maxQueueSize, maxQueueChannel);
                Log(LogLevel.Information, "Sending EnqueueMessage {MessageID} of type {MessageType}", msg.ID, Utility.TypeName<T>());
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
                }, connectionOptions.GrpcMetadata, cancellationToken);
                Log(LogLevel.Debug, "Transmission Result for EnqueueMessage {MessageID} (IsError:{IsError},Error:{ErrorMessage})", msg.ID, !string.IsNullOrEmpty(res.Error), res.Error);
                return new TransmissionResult(id:new Guid(msg.ID),error:res.Error);
            }
            catch (RpcException ex)
            {
                Log(LogLevel.Error, "RPC error occured on EnqueueMessage in send Message:{ErrorMessage}, Status: {StatusCode}", ex.Message, ex.Status);
                return new TransmissionResult(error: $"Status: {ex.Status}, Message: {ex.Message}");
            }
            catch (Exception ex)
            {
                Log(LogLevel.Error, "Exception occured in EnqueueMessage Message:{ErrorMessage}", ex.Message);
                return new TransmissionResult(error:ex.Message);
            }
        }

        public async Task<IBatchTransmissionResult> EnqueueMessages<T>(IEnumerable<T> messages, string? channel = null, Dictionary<string, string>? tagCollection = null, int? expirationSeconds = null, int? delaySeconds = null, int? maxQueueSize = null, string? maxQueueChannel = null, CancellationToken cancellationToken = new CancellationToken())
        {
            try
            {
                var msg = GetMessageFactory<T>().Enqueue(messages, connectionOptions,clientID, channel, tagCollection, delaySeconds, expirationSeconds, maxQueueSize, maxQueueChannel);
                Log(LogLevel.Information, "Sending EnqueueMessages {MessageID} of type {MessageType}", msg.ID, Utility.TypeName<T>());
                var res = await client.SendQueueMessagesBatchAsync(
                    new QueueMessagesBatchRequest()
                    {
                        BatchID=msg.ID.ToString(),
                        Messages={ msg.Messages }
                    }, connectionOptions.GrpcMetadata, cancellationToken);
                if (res==null)
                {
                    Log(LogLevel.Error, "EnqueueMessages response for {MessageID} is null from KubeMQ server", msg.ID);
                    return new BatchTransmissionResult(id:msg.ID,results:new ITransmissionResult[]
                        {
                            new TransmissionResult(error:"null response recieved from KubeMQ host")
                        }
                    );
                }
                Log(LogLevel.Debug, "Transmission Result for EnqueueMessages {MessageID} (Count:{MessageCount})", msg.ID, res.Results.Count);
                return new BatchTransmissionResult(id:msg.ID,
                    results:res.Results.AsEnumerable<SendQueueMessageResult>().Select(sqmr =>new TransmissionResult(id: new Guid(sqmr.MessageID),error:sqmr.Error))
                );
            }
            catch (RpcException ex)
            {
                Log(LogLevel.Error, "RPC error occured on EnqueueMessages in send Message:{ErrorMessage}, Status: {StatusCode}", ex.Message, ex.Status);
                return new BatchTransmissionResult(error:$"Status: {ex.Status}, Message: {ex.Message}");
            }
            catch (Exception ex)
            {
                Log(LogLevel.Error, "Exception occured in EnqueueMessages Message:{ErrorMessage}", ex.Message);
                return new BatchTransmissionResult(error:ex.Message);
            }
        }

        public IMessageQueue<T> SubscribeToQueue<T>(string? channel = null, bool ignoreMessageHeader = false, CancellationToken cancellationToken = default)
        {
            Log(LogLevel.Information, "Requesting SubscribeToQueue of type {MessageType}", Utility.TypeName<T>());
            var id = Guid.NewGuid();
            return new MessageQueue<T>(id,GetMessageFactory<T>(ignoreMessageHeader), connectionOptions, EstablishConnection(), ProduceLogger(id), channel,cancellationToken);
        }

        public IReadonlyMessageStream<T> SubscribeToQueueAsStream<T>(string? channel = null, bool ignoreMessageHeader = false, CancellationToken cancellationToken = default)
        {
            Log(LogLevel.Information, "Requesting SubscribeToQueue of type {MessageType}", Utility.TypeName<T>());
            var id = Guid.NewGuid();
            return new ReadonlyMessageQueueStream<T>(id, GetMessageFactory<T>(ignoreMessageHeader), connectionOptions, EstablishConnection(), ProduceLogger(id), channel,cancellationToken);
        }
    }
}
