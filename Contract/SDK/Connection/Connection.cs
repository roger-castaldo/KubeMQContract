using Google.Protobuf;
using Grpc.Core;
using KubeMQ.Contract.Factories;
using KubeMQ.Contract.Interfaces;
using KubeMQ.Contract.Interfaces.Connections;
using KubeMQ.Contract.Interfaces.Messages;
using KubeMQ.Contract.Messages;
using KubeMQ.Contract.SDK.Grpc;
using KubeMQ.Contract.Subscriptions;
using Microsoft.Extensions.Logging;

namespace KubeMQ.Contract.SDK.Connection
{
    internal partial class Connection : IConnection, IDisposable
    {
        private readonly ConnectionOptions connectionOptions;
        private readonly IGlobalMessageEncoder? globalMessageEncoder;
        private readonly IGlobalMessageEncryptor? globalMessageEncryptor;
        private readonly kubemq.kubemqClient client;
        private readonly List<IMessageSubscription> subscriptions;
        private readonly ReaderWriterLockSlim dataLock = new();
        private IEnumerable<object> typeFactories;

        public Connection(ConnectionOptions connectionOptions, IGlobalMessageEncoder? globalMessageEncoder, IGlobalMessageEncryptor? globalMessageEncryptor)
        {
            this.connectionOptions = connectionOptions;
            this.globalMessageEncoder = globalMessageEncoder;
            this.globalMessageEncryptor=globalMessageEncryptor;
            Log(LogLevel.Debug, "Attempting to establish connection to server {}", connectionOptions.Address);
            Channel channel;
            var sslCreds = connectionOptions.SSLCredentials;
            channel = new Channel(this.connectionOptions.Address, (sslCreds??ChannelCredentials.Insecure));
            client = new kubemq.kubemqClient(channel);
            subscriptions = new();
            typeFactories = Array.Empty<object>();
        }

        private IMessageFactory<T> GetMessageFactory<T>()
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
    }
}
