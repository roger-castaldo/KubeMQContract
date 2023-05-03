using Google.Protobuf;
using Grpc.Core;
using KubeMQ.Contract.Factories;
using KubeMQ.Contract.Interfaces;
using KubeMQ.Contract.Messages;
using KubeMQ.Contract.SDK.Messages;
using KubeMQ.Contract.Subscriptions;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace KubeMQ.Contract.SDK.Grpc
{
    internal class Connection : IConnection, ILogProvider, IDisposable
    {
        private readonly ConnectionOptions connectionOptions;
        private readonly IGlobalMessageEncoder? globalMessageEncoder;
        private readonly IGlobalMessageEncryptor? globalMessageEncryptor;
        private readonly kubemq.kubemqClient client;
        private readonly List<IMessageSubscription> subscriptions;
        private readonly ReaderWriterLockSlim dataLock = new ReaderWriterLockSlim();
        private IEnumerable<object> typeFactories;

        public Connection(ConnectionOptions connectionOptions, IGlobalMessageEncoder? globalMessageEncoder, IGlobalMessageEncryptor? globalMessageEncryptor)
        {
            this.connectionOptions = connectionOptions;
            this.globalMessageEncoder = globalMessageEncoder;
            this.globalMessageEncryptor=globalMessageEncryptor;
            log(LogLevel.Debug, "Attempting to establish connection to server {}", connectionOptions.Address);
            Channel channel;
            var sslCreds = connectionOptions.SSLCredentials;
            channel = new Channel(this.connectionOptions.Address, (sslCreds==null ? ChannelCredentials.Insecure : sslCreds));
            client = new kubemq.kubemqClient(channel);
            subscriptions = new();
            typeFactories = Array.Empty<object>();
        }

        public IPingResult Ping()
        {
            log(LogLevel.Information, "Calling ping to {}", connectionOptions.Address);
            var rec = this.client.Ping(new Empty());
            log(LogLevel.Information, "Pind result to {} Uptime seconds {}", connectionOptions.Address, rec.ServerUpTimeSeconds);
            return new KubeMQ.Contract.SDK.PingResult(rec);
        }

        private IMessageFactory<T> getMessageFactory<T>()
        {
            dataLock.EnterReadLock();
            var result = (IMessageFactory<T>?)typeFactories.FirstOrDefault(fact => fact.GetType().GetGenericArguments()[0]==typeof(T));
            dataLock.ExitReadLock();
            if (result==null)
            {
                result = new TypeFactory<T>(globalMessageEncoder,globalMessageEncryptor);
                dataLock.EnterWriteLock();
                if (!typeFactories.Any(fact => fact.GetType().GetGenericArguments()[0]==typeof(T)))
                    typeFactories = typeFactories.Append(result);
                dataLock.ExitWriteLock();
            }
            return result;
        }

        public async Task<ITransmissionResult> Send<T>(T message, CancellationToken cancellationToken = new CancellationToken(), string? channel = null, Dictionary<string, string>? tagCollection = null)
        {
            try
            {
                var msg = getMessageFactory<T>().Event(message,connectionOptions, channel, tagCollection);
                log(LogLevel.Information, "Sending Message {} of type {}",msg.ID, typeof(T).Name);
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
                log(LogLevel.Information, "Transmission Result for {} (IsError:{},Error:{})", msg.ID, !string.IsNullOrEmpty(res.Error), res.Error);
                return new TransmissionResult()
                {
                    MessageID=new Guid(msg.ID),
                    IsError = !string.IsNullOrEmpty(res.Error),
                    Error=res.Error
                };
            }
            catch (RpcException ex)
            {
                log(LogLevel.Error, "RPC error occured on Send in send Message:{}, Status: {}", ex.Message, ex.Status);
                return new TransmissionResult()
                {
                    IsError=true,
                    Error=$"Message: {ex.Message}, Status: {ex.Status}"
                };
            }
            catch (Exception ex)
            {
                log(LogLevel.Error, "Exception occured in Send Message:{}", ex.Message);
                return new TransmissionResult()
                {
                    IsError=true,
                    Error=ex.Message
                };
            }
        }

        public async Task<Contract.Interfaces.IResultMessage<R>> SendRPC<T, R>(T message, CancellationToken cancellationToken = new CancellationToken(), string? channel = null, Dictionary<string, string>? tagCollection = null, int? timeout = null, RPCType? type = null)
        {
            try
            {
                var msg = getMessageFactory<T>().Request<R>(message, connectionOptions, channel, tagCollection,timeout,type);
                log(LogLevel.Information, "Sending RPC Message {} of type {}", msg.ID, typeof(T).Name);
                var res = await client.SendRequestAsync(new Request()
                {
                    RequestID=msg.ID,
                    RequestTypeData = msg.CommandType,
                    Timeout = msg.Timeout,
                    ClientID = msg.ClientID,
                    Channel = msg.Channel,
                    Metadata = msg.MetaData,
                    Body = ByteString.CopyFrom(msg.Body),
                    Tags = { msg.Tags }
                }, connectionOptions.GrpcMetadata, null, cancellationToken);
                if (res==null)
                {
                    log(LogLevel.Error, "Transmission Result for RPC {} is null", msg.ID);
                    return new ResultMessage<R>()
                    {
                        IsError=true,
                        Error="null response recieved from KubeMQ server"
                    };
                }
                log(LogLevel.Information, "Transmission Result for RPC {} (IsError:{},Error:{})", msg.ID, !string.IsNullOrEmpty(res.Error), res.Error);
                if (!res.Executed || !string.IsNullOrEmpty(res.Error))
                    return new ResultMessage<R>()
                    {
                        IsError=true,
                        Error=res.Error
                    };
                return getMessageFactory<R>().ConvertMessage(this, res);
            }
            catch (RpcException ex)
            {
                log(LogLevel.Error, "RPC error occured on SendRPC in send Message:{}, Status: {}", ex.Message, ex.Status);
                return new ResultMessage<R>()
                {
                    IsError=true,
                    Error=$"Message: {ex.Message}, Status: {ex.Status}"
                };
            }
            catch (Exception ex)
            {
                log(LogLevel.Error, "Exception occured in SendRPC Message:{}, Status: {}", ex.Message);
                return new ResultMessage<R>()
                {
                    IsError=true,
                    Error=ex.Message
                };
            }
        }

        public async Task<ITransmissionResult> EnqueueMessage<T>(T message, CancellationToken cancellationToken = new CancellationToken(), string? channel = null, Dictionary<string, string>? tagCollection = null, int? expirationSeconds = null, int? delaySeconds = null, int? maxQueueSize = null, string? maxQueueChannel = null)
        {
            try
            {
                var msg = getMessageFactory<T>().Enqueue(message, connectionOptions, channel, tagCollection, delaySeconds, expirationSeconds, maxQueueSize, maxQueueChannel);
                log(LogLevel.Information, "Sending EnqueueMessage {} of type {}", msg.ID, typeof(T).Name);
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
                log(LogLevel.Information, "Transmission Result for EnqueueMessage {} (IsError:{},Error:{})", msg.ID, !string.IsNullOrEmpty(res.Error), res.Error);
                return new TransmissionResult()
                {
                    MessageID=new Guid(msg.ID),
                    IsError = !string.IsNullOrEmpty(res.Error),
                    Error=res.Error
                };
            }
            catch (RpcException ex)
            {
                log(LogLevel.Error, "RPC error occured on EnqueueMessage in send Message:{}, Status: {}", ex.Message, ex.Status);
                return new TransmissionResult()
                {
                    IsError=true,
                    Error=$"Message: {ex.Message}, Status: {ex.Status}"
                };
            }
            catch (Exception ex)
            {
                log(LogLevel.Error, "Exception occured in EnqueueMessage Message:{}, Status: {}", ex.Message);
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
                var msg = getMessageFactory<T>().Enqueue(messages, connectionOptions, channel, tagCollection, delaySeconds, expirationSeconds, maxQueueSize, maxQueueChannel);
                log(LogLevel.Information, "Sending EnqueueMessages {} of type {}", msg.ID, typeof(T).Name);
                var res = await client.SendQueueMessagesBatchAsync(
                    new QueueMessagesBatchRequest()
                    {
                        BatchID=msg.ID.ToString(),
                        Messages={ msg.Messages }
                    }, connectionOptions.GrpcMetadata, null, cancellationToken);
                log(LogLevel.Information, "Transmission Result for EnqueueMessages {} (Count:{})", msg.ID, res.Results.Count());
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
                log(LogLevel.Error, "RPC error occured on EnqueueMessages in send Message:{}, Status: {}", ex.Message, ex.Status);
                return new BatchTransmissionResult()
                {
                    IsError=true,
                    Error=$"Message: {ex.Message}, Status: {ex.Status}"
                };
            }
            catch (Exception ex)
            {
                log(LogLevel.Error, "Exception occured in EnqueueMessages Message:{}, Status: {}", ex.Message);
                return new BatchTransmissionResult()
                {
                    IsError=true,
                    Error=ex.Message
                };
            }
        }

        public Guid Subscribe<T>(Action<Contract.Interfaces.IMessage<T>> messageRecieved, Action<string> errorRecieved, CancellationToken cancellationToken = new CancellationToken(), string? channel = null, string group = "", long storageOffset = 0, MessageReadStyle? messageReadStyle = null)
        {
            var sub = new EventSubscription<T>(getMessageFactory<T>(),new KubeSubscription<T>(this.connectionOptions, channel: channel, group: group), this.client, this.connectionOptions, messageRecieved, errorRecieved, cancellationToken, storageOffset,this, messageReadStyle);
            log(LogLevel.Information, "Requesting Subscribe {} of type {}", sub.ID, typeof(T).Name);
            lock (subscriptions)
            {
                subscriptions.Add(sub);
            }
            return sub.ID;
        }

        public Guid SubscribeRPC<T, R>(
            Func<Contract.Interfaces.IMessage<T>, TaggedResponse<R>> processMessage,
            Action<string> errorRecieved,
            CancellationToken cancellationToken = new CancellationToken(),
            string? channel = null,
            string group = "",
            RPCType? commandType = null
        )
        {
            var sub = new RPCSubscription<T, R>(getMessageFactory<T>(),getMessageFactory<R>(),new KubeSubscription<T>(this.connectionOptions, channel: channel, group: group),
                this.client,
                this.connectionOptions, processMessage, errorRecieved, cancellationToken, 
                this, commandType: commandType);
            log(LogLevel.Information, "Requesting SubscribeRPC {} of type {}", sub.ID, typeof(T).Name);
            lock (subscriptions)
            {
                subscriptions.Add(sub);
            }
            return sub.ID;
        }

        public IMessageQueue<T> SubscribeToQueue<T>(CancellationToken cancellationToken = default, string? channel = null)
        {
            log(LogLevel.Information, "Requesting SubscribeToQueue of type {}", typeof(T).Name);
            return new MessageQueue<T>(getMessageFactory<T>(),connectionOptions, client,this, channel);
        }

        public void Unsubscribe(Guid id)
        {
            log(LogLevel.Information, "Unsubscribing from {}", id);
            lock (subscriptions)
            {
                var sub = subscriptions.FirstOrDefault(s => s.ID == id);
                if (sub!=null)
                {
                    sub.Stop();
                    subscriptions.Remove(sub);
                }
            }
        }

        public void Dispose()
        {
            lock (subscriptions)
            {
                foreach (var sub in subscriptions)
                {
                    sub.Stop();
                }
                subscriptions.Clear();
            }
            dataLock.Dispose();
        }

        #region ILogProvider
        private void log(LogLevel level, string message, params object[]? args)
        {
            if (connectionOptions.Logger!=null)
                connectionOptions.Logger.Log(level, message, args);
        }

        void ILogProvider.LogInformation(string message, params object[]? args)
        {
            log(LogLevel.Information, message, args);
        }

        void ILogProvider.LogTrace(string message, params object[]? args)
        {
            log(LogLevel.Trace, message, args);
        }

        void ILogProvider.LogWarning(string message, params object[]? args)
        {
            log(LogLevel.Warning, message, args);
        }

        void ILogProvider.LogDebug(string message, params object[]? args)
        {
            log(LogLevel.Debug, message, args);
        }

        void ILogProvider.LogError(string message, params object[]? args)
        {
            log(LogLevel.Error, message, args);
        }

        void ILogProvider.LogCritical(string message, params object[]? args)
        {
            log(LogLevel.Information, message, args);
        }
        #endregion
    }
}
