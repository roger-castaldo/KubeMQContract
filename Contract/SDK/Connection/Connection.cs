using Grpc.Core;
using KubeMQ.Contract.Factories;
using KubeMQ.Contract.Interfaces;
using KubeMQ.Contract.Interfaces.Connections;
using KubeMQ.Contract.Interfaces.Messages;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace KubeMQ.Contract.SDK.Connection
{
    internal partial class Connection : IConnection, IDisposable
    {
        private static readonly Regex regURL = new("^http(s)?://(.+)$", RegexOptions.Compiled|RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(500));

        private readonly Guid id;
        private readonly string clientID;
        private readonly ConnectionOptions connectionOptions;
        private readonly IMessageEncoder? globalMessageEncoder;
        private readonly IMessageEncryptor? globalMessageEncryptor;
        private readonly KubeClient client;
        private readonly List<IMessageSubscription> subscriptions;
        private readonly ReaderWriterLockSlim dataLock = new();
        private IEnumerable<ITypeFactory> typeFactories;
        private bool disposedValue;
        private readonly string addy;
        private readonly int messageSize;
        private readonly ILogger? logger;

        public Connection(ConnectionOptions connectionOptions, IMessageEncoder? globalMessageEncoder, IMessageEncryptor? globalMessageEncryptor)
        {
            if ((long)connectionOptions.MaxBodySize+4096 > (long)int.MaxValue)
                throw new ArgumentException($"The maximum body size is too large, it cannot exceed {int.MaxValue-4096}");
            id=Guid.NewGuid();
            clientID = $"{connectionOptions.ClientId}[{id}]";
            this.connectionOptions = connectionOptions;
            this.globalMessageEncoder = globalMessageEncoder;
            this.globalMessageEncryptor=globalMessageEncryptor;
            messageSize=connectionOptions.MaxBodySize+4096;
            logger = connectionOptions.Logger?.CreateLogger($"KubeMQContract[{id}]");
            Log(LogLevel.Information, "Attempting to establish connection to server {}", connectionOptions.Address);
            addy = this.connectionOptions.Address;
            var match = regURL.Match(addy);
            if (!match.Success)
                addy=$"http{(connectionOptions.SSLCredentials!=null ? "s" : "")}://{addy}";
            else if (connectionOptions.SSLCredentials!=null && string.IsNullOrEmpty(match.Groups[1].Value))
                addy = $"https://{match.Groups[2].Value}";
            else if (connectionOptions.SSLCredentials==null && !string.IsNullOrEmpty(match.Groups[1].Value))
                addy = $"http://{match.Groups[2].Value}";
            subscriptions = new();
            typeFactories = Array.Empty<ITypeFactory>();
            dataLock.EnterWriteLock();
            try
            {
                client = EstablishConnection();
            }
            catch (Exception)
            {
                dataLock.ExitWriteLock();
                throw;
            }
            dataLock.ExitWriteLock();
        }

        private void Log(LogLevel level, string message, params object[]? args)
            =>
#pragma warning disable CA2254 // Template should be a static expression
            logger?.Log(level, message, args);
#pragma warning restore CA2254 // Template should be a static expression

        private KubeClient EstablishConnection()
        {
            var client = new KubeClient(addy, connectionOptions.SSLCredentials??ChannelCredentials.Insecure, messageSize, logger);
            var pingResult = Ping(client)??throw new UnableToConnect();
            Log(LogLevel.Information, "Established connection to [Host:{}, Version:{}, StartTime:{}, UpTime:{}]",
                pingResult.Host,
                pingResult.Version,
                pingResult.ServerStartTime,
                pingResult.ServerUpTime
            );
            return client;
        }

        private IMessageFactory<T> GetMessageFactory<T>(bool ignoreMessageHeader=false) 
        {
            dataLock.EnterReadLock();
            var result = (IMessageFactory<T>?)typeFactories.FirstOrDefault(fact => fact.GetType().GetGenericArguments()[0]==typeof(T));
            dataLock.ExitReadLock();
            if (result==null)
            {
                result = new TypeFactory<T>(globalMessageEncoder,globalMessageEncryptor,ignoreMessageHeader);
                dataLock.EnterWriteLock();
                if (!typeFactories.Any(fact => fact.GetType().GetGenericArguments()[0]==typeof(T) && fact.IgnoreMessageHeader==ignoreMessageHeader))
                    typeFactories = typeFactories.Concat(new ITypeFactory[] { (ITypeFactory)result });
                dataLock.ExitWriteLock();
            }
            return result;
        }

        private T RegisterSubscription<T>(T sub)
            where T : IMessageSubscription
        {
            sub.Start();
            dataLock.EnterWriteLock();
            subscriptions.Add(sub);
            dataLock.ExitWriteLock();
            return sub;
        }

        private ILogger? ProduceLogger(Guid subID)
            =>connectionOptions.Logger?.CreateLogger($"KubeMQContract[Conn({this.id}),Sub({subID})]");

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    dataLock.EnterWriteLock();
                    foreach (var sub in subscriptions)
                        sub.Stop();
                    subscriptions.Clear();
                    client.Dispose();
                    dataLock.ExitWriteLock();
                    dataLock.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue=true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~Connection()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
