using Google.Protobuf;
using Grpc.Core;
using KubeMQ.Contract.Interfaces;
using KubeMQ.Contract.Messages;
using KubeMQ.Contract.SDK.Messages;
using KubeMQ.Contract.Subscriptions;
using System;
using System.Linq;
using System.Reflection;

namespace KubeMQ.Contract.SDK.Grpc
{
    internal class Connection : IConnection,IDisposable
    {
        private readonly ConnectionOptions connectionOptions;
        private readonly kubemq.kubemqClient client;
        private readonly List<IMessageSubscription> subscriptions;

        public Connection(ConnectionOptions connectionOptions)
        {
            this.connectionOptions = connectionOptions;
            Channel channel;
            var sslCreds = connectionOptions.SSLCredentials;
            channel = new Channel(this.connectionOptions.Address,(sslCreds==null ? ChannelCredentials.Insecure : sslCreds));
            this.client = new kubemq.kubemqClient(channel);
            this.subscriptions = new();
        }

        public IPingResult Ping()
        {
            var rec = this.client.Ping(new Empty());
            return new KubeMQ.Contract.SDK.PingResult(rec);
        }

        public async Task<ITransmissionResult> Send<T>(T message,CancellationToken cancellationToken = new CancellationToken(), string? channel=null){
            ConverterFactory.RegisterAssembly(Assembly.GetCallingAssembly());
            try
            {
                var msg = new KubeEvent<T>(message, connectionOptions, channel);
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
                return new TransmissionResult()
                {
                    MessageID=new Guid(msg.ID),
                    IsError = !string.IsNullOrEmpty(res.Error),
                    Error=res.Error
                };
            }
            catch (RpcException ex)
            {
                return new TransmissionResult()
                {
                    IsError=true,
                    Error=$"Message: {ex.Message}, Status: {ex.Status}"
                };
            }
            catch (Exception ex)
            {
                return new TransmissionResult()
                {
                    IsError=true,
                    Error=ex.Message
                };
            }
        }

        public async Task<Contract.Interfaces.IMessage<R>> SendRPC<T, R>(T message, CancellationToken cancellationToken = new CancellationToken(), string? channel = null, int? timeout = null, RPCType? type = null)
        {
            ConverterFactory.RegisterAssembly(Assembly.GetCallingAssembly());
            try
            {
                var msg = new KubeRequest<T,R>(message, connectionOptions,timeout:timeout,channel:channel);
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
                if (res==null || !res.Executed || !string.IsNullOrEmpty(res.Error))
                    return new Message<R>()
                    {
                        IsError=true,
                        Error=res.Error
                    };
                return new Message<R>()
                {
                    Data = Utility.ConvertMessage<R>(res)
                };
            }
            catch (RpcException ex)
            {
                return new Message<R>()
                {
                    IsError=true,
                    Error=$"Message: {ex.Message}, Status: {ex.Status}"
                };
            }
            catch (Exception ex)
            {
                return new Message<R>()
                {
                    IsError=true,
                    Error=ex.Message
                };
            }
        }

        public async Task<ITransmissionResult> EnqueueMessage<T>(T message, CancellationToken cancellationToken = new CancellationToken(), string? channel = null, int? expirationSeconds = null, int? delaySeconds = null, int? maxQueueSize = null, string? maxQueueChannel = null)
        {
            ConverterFactory.RegisterAssembly(Assembly.GetCallingAssembly());
            try
            {
                var msg = new KubeEnqueue<T>(message, connectionOptions, channel:channel,delaySeconds:delaySeconds,expirationSeconds:expirationSeconds,maxCount:maxQueueSize,maxCountChannel:maxQueueChannel);
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
                return new TransmissionResult()
                {
                    MessageID=new Guid(msg.ID),
                    IsError = !string.IsNullOrEmpty(res.Error),
                    Error=res.Error
                };
            }
            catch (RpcException ex)
            {
                return new TransmissionResult()
                {
                    IsError=true,
                    Error=$"Message: {ex.Message}, Status: {ex.Status}"
                };
            }
            catch (Exception ex)
            {
                return new TransmissionResult()
                {
                    IsError=true,
                    Error=ex.Message
                };
            }
        }

        public async Task<IBatchTransmissionResult> EnqueueMessages<T>(IEnumerable<T> messages, CancellationToken cancellationToken = new CancellationToken(), string? channel = null, int? expirationSeconds = null, int? delaySeconds = null, int? maxQueueSize = null, string? maxQueueChannel = null)
        {
            ConverterFactory.RegisterAssembly(Assembly.GetCallingAssembly());
            try
            {
                var msg = new KubeBatchEnqueue<T>(messages, connectionOptions, channel: channel, delaySeconds: delaySeconds, expirationSeconds: expirationSeconds, maxCount: maxQueueSize, maxCountChannel: maxQueueChannel);
                var res = await client.SendQueueMessagesBatchAsync(
                    new QueueMessagesBatchRequest()
                    {
                        BatchID=msg.ID.ToString(),
                        Messages={ msg.Messages }
                    }, connectionOptions.GrpcMetadata, null, cancellationToken);
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
                return new BatchTransmissionResult()
                {
                    IsError=true,
                    Error=$"Message: {ex.Message}, Status: {ex.Status}"
                };
            }
            catch (Exception ex)
            {
                return new BatchTransmissionResult()
                {
                    IsError=true,
                    Error=ex.Message
                };
            }
        }

        public Guid Subscribe<T>(Action<T> messageRecieved,Action<string> errorRecieved, CancellationToken cancellationToken = new CancellationToken(),string? channel=null,string group = "", long storageOffset = 0)
        {
            ConverterFactory.RegisterAssembly(Assembly.GetCallingAssembly());
            var sub = new EventSubscription<T>(new KubeSubscription(typeof(T),this.connectionOptions,channel:channel,group:group),this.client,this.connectionOptions,messageRecieved,errorRecieved,cancellationToken,storageOffset);
            lock (subscriptions)
            {
                subscriptions.Add(sub);
            }
            return sub.ID;
        }

        public Guid SubscribeRPC<T, R>(
            Func<T, R> processMessage,
            Action<string> errorRecieved,
            CancellationToken cancellationToken = new CancellationToken(),
            string? channel = null,
            string group = "",
            RPCType? commandType = null
        )
        {
            ConverterFactory.RegisterAssembly(Assembly.GetCallingAssembly());
            var sub = new RPCSubscription<T,R>(new KubeSubscription(typeof(T), this.connectionOptions, channel: channel, group: group), 
                this.client, 
                this.connectionOptions, processMessage, errorRecieved, cancellationToken, commandType:commandType);
            lock (subscriptions)
            {
                subscriptions.Add(sub);
            }
            return sub.ID;
        }

        public IMessageQueue<T> SubscribeToQueue<T>(CancellationToken cancellationToken = default, string? channel = null)
        {
            ConverterFactory.RegisterAssembly(Assembly.GetCallingAssembly());
            return new MessageQueue<T>(connectionOptions, client, channel: channel);
        }

        public void Unsubscribe(Guid id)
        {
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
    }
}
