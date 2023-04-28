using Google.Protobuf;
using Grpc.Core;
using KubeMQ.Contract.Interfaces;
using KubeMQ.Contract.Messages;
using KubeMQ.Contract.SDK.Messages;
using KubeMQ.Contract.Subscriptions;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Reflection;

namespace KubeMQ.Contract.SDK.Grpc
{
    internal class Connection : IConnection, ILogProvider, IDisposable
    {
        private readonly ConnectionOptions connectionOptions;
        private readonly kubemq.kubemqClient client;
        private readonly List<IMessageSubscription> subscriptions;

        public Connection(ConnectionOptions connectionOptions)
        {
            this.connectionOptions = connectionOptions;
            log(LogLevel.Debug, "Attempting to establish connection to server {}", connectionOptions.Address);
            Channel channel;
            var sslCreds = connectionOptions.SSLCredentials;
            channel = new Channel(this.connectionOptions.Address, (sslCreds==null ? ChannelCredentials.Insecure : sslCreds));
            this.client = new kubemq.kubemqClient(channel);
            this.subscriptions = new();
        }

        private void registerAssembly(Assembly assembly)
        {
            ConverterFactory.RegisterAssembly(this, assembly);
            EncoderFactory.RegisterAssembly(this, assembly);
        }

        public IPingResult Ping()
        {
            log(LogLevel.Information, "Calling ping to {}", connectionOptions.Address);
            var rec = this.client.Ping(new Empty());
            log(LogLevel.Information, "Pind result to {} Uptime seconds {}", connectionOptions.Address, rec.ServerUpTimeSeconds);
            return new KubeMQ.Contract.SDK.PingResult(rec);
        }

        public async Task<ITransmissionResult> Send<T>(T message, CancellationToken cancellationToken = new CancellationToken(), string? channel = null, Dictionary<string, string>? tagCollection = null)
        {
            registerAssembly(Assembly.GetCallingAssembly());
            try
            {
                var msg = new KubeEvent<T>(message, connectionOptions, channel,tagCollection);
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
                log(LogLevel.Error, "Exception occured in Send Message:{}, Status: {}", ex.Message);
                return new TransmissionResult()
                {
                    IsError=true,
                    Error=ex.Message
                };
            }
        }

        public async Task<Contract.Interfaces.IResultMessage<R>> SendRPC<T, R>(T message, CancellationToken cancellationToken = new CancellationToken(), string? channel = null, int? timeout = null, RPCType? type = null, Dictionary<string, string>? tagCollection = null)
        {
            registerAssembly(Assembly.GetCallingAssembly());
            try
            {
                var msg = new KubeRequest<T, R>(message, connectionOptions, timeout, channel,type,tagCollection);
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
                log(LogLevel.Information, "Transmission Result for RPC {} (IsError:{},Error:{})", msg.ID, !string.IsNullOrEmpty(res.Error), res.Error);
                if (res==null || !res.Executed || !string.IsNullOrEmpty(res.Error))
                    return new ResultMessage<R>()
                    {
                        IsError=true,
                        Error=res.Error
                    };
                return Utility.ConvertMessage<R>(this,res);
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

        public async Task<ITransmissionResult> EnqueueMessage<T>(T message, CancellationToken cancellationToken = new CancellationToken(), string? channel = null, int? expirationSeconds = null, int? delaySeconds = null, int? maxQueueSize = null, string? maxQueueChannel = null, Dictionary<string, string>? tagCollection = null)
        {
            registerAssembly(Assembly.GetCallingAssembly());
            try
            {
                var msg = new KubeEnqueue<T>(message, connectionOptions, channel, delaySeconds, expirationSeconds, maxQueueSize, maxQueueChannel,tagCollection);
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

        public async Task<IBatchTransmissionResult> EnqueueMessages<T>(IEnumerable<T> messages, CancellationToken cancellationToken = new CancellationToken(), string? channel = null, int? expirationSeconds = null, int? delaySeconds = null, int? maxQueueSize = null, string? maxQueueChannel = null, Dictionary<string, string>? tagCollection = null)
        {
            registerAssembly(Assembly.GetCallingAssembly());
            try
            {
                var msg = new KubeBatchEnqueue<T>(messages, connectionOptions, channel, delaySeconds, expirationSeconds, maxQueueSize, maxQueueChannel,tagCollection);
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

        public Guid Subscribe<T>(Action<Contract.Interfaces.IMessage<T>> messageRecieved, Action<string> errorRecieved, CancellationToken cancellationToken = new CancellationToken(), string? channel = null, string group = "", long storageOffset = 0)
        {
            registerAssembly(Assembly.GetCallingAssembly());
            var sub = new EventSubscription<T>(new KubeSubscription(typeof(T), this.connectionOptions, channel: channel, group: group), this.client, this.connectionOptions, messageRecieved, errorRecieved, cancellationToken, storageOffset,this);
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
            registerAssembly(Assembly.GetCallingAssembly());
            var sub = new RPCSubscription<T, R>(new KubeSubscription(typeof(T), this.connectionOptions, channel: channel, group: group),
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
            registerAssembly(Assembly.GetCallingAssembly());
            log(LogLevel.Information, "Requesting SubscribeToQueue of type {}", typeof(T).Name);
            return new MessageQueue<T>(connectionOptions, client,this, channel);
        }

        public void Unsubscribe(Guid id)
        {
            log(LogLevel.Information, "Unsubscribing from {}", id);
            lock (subscriptions)
            {
                IMessageSubscription sub = subscriptions.FirstOrDefault(s => s.ID == id);
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
