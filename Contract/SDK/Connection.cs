using Google.Protobuf;
using Grpc.Core;
using KubeMQ.Contract.Interfaces;
using System;

namespace KubeMQ.Contract.SDK.Grpc
{
    internal class Connection : IConnection
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

        public ITransmissionResult Send<T>(T message,CancellationToken cancellationToken = new CancellationToken(), string? channel=null){
            try
            {
                var msg = new KubeMessage<T>(message, connectionOptions, channel);
                var res = client.SendEvent(new Event
                {
                    EventID = msg.ID,
                    ClientID = msg.ClientID,
                    Channel = msg.Channel,
                    Metadata = msg.MetaData,
                    Body = ByteString.CopyFrom(msg.Body),
                    Store = msg.Stored,
                    Tags = { msg.Tags }
                }, connectionOptions.GrpcMetadata, null, cancellationToken);
                return new MessageResult()
                {
                    MessageID=new Guid(msg.ID),
                    IsError = !string.IsNullOrEmpty(res.Error),
                    Error=res.Error
                };
            }
            catch (RpcException ex)
            {
                return new MessageResult()
                {
                    IsError=true,
                    Error=$"Message: {ex.Message}, Status: {ex.Status}"
                };
            }
            catch (Exception ex)
            {
                return new MessageResult()
                {
                    IsError=true,
                    Error=ex.Message
                };
            }
        }

        public Guid Subscribe<T>(Action<T> messageRecieved,Action<string> errorRecieved, CancellationToken cancellationToken = new CancellationToken(),string? channel=null,string group = "", long storageOffset = 0)
        {
            var sub = new EventSubscription<T>(new KubeSubscription(typeof(T),this.connectionOptions,channel:channel,group:group),this.client,this.connectionOptions,messageRecieved,errorRecieved,cancellationToken,storageOffset);
            lock (subscriptions)
            {
                subscriptions.Add(sub);
            }
            return sub.ID;
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
    }
}
