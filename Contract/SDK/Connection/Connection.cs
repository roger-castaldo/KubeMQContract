﻿using Grpc.Core;
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
        private static readonly Regex _regURL = new("^http(s)?://(.+)$", RegexOptions.Compiled|RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(500));

        private readonly Guid id;
        private readonly string clientID;
        private readonly ConnectionOptions connectionOptions;
        private readonly IGlobalMessageEncoder? globalMessageEncoder;
        private readonly IGlobalMessageEncryptor? globalMessageEncryptor;
        private readonly KubeClient client;
        private readonly List<IMessageSubscription> subscriptions;
        private readonly ReaderWriterLockSlim dataLock = new();
        private IEnumerable<ITypeFactory> typeFactories;
        private readonly string addy;
        private readonly ILogger? logger;

        public Connection(ConnectionOptions connectionOptions, IGlobalMessageEncoder? globalMessageEncoder, IGlobalMessageEncryptor? globalMessageEncryptor)
        {
            id=Guid.NewGuid();
            clientID = $"{connectionOptions.ClientId}[{id}]";
            this.connectionOptions = connectionOptions;
            this.globalMessageEncoder = globalMessageEncoder;
            this.globalMessageEncryptor=globalMessageEncryptor;
            this.logger = connectionOptions.Logger?.CreateLogger($"KubeMQContract[{id}]");
            Log(LogLevel.Debug, "Attempting to establish connection to server {}", connectionOptions.Address);
            addy = this.connectionOptions.Address;
            var match = _regURL.Match(addy);
            if (!match.Success)
                addy=$"http{(connectionOptions.SSLCredentials!=null ? "s" : "")}://{addy}";
            else
            {
                if (connectionOptions.SSLCredentials!=null && string.IsNullOrEmpty(match.Groups[1].Value))
                    addy = $"https://{match.Groups[2].Value}";
                else if (connectionOptions.SSLCredentials==null && !string.IsNullOrEmpty(match.Groups[1].Value))
                    addy = $"http://{match.Groups[2].Value}";
            }
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
        {
#pragma warning disable CA2254 // Template should be a static expression
            logger?.Log(level, message, args);
#pragma warning restore CA2254 // Template should be a static expression
        }

        private KubeClient EstablishConnection()
        {
            var client = new KubeClient(addy, connectionOptions.SSLCredentials??ChannelCredentials.Insecure,logger);
            var pingResult = Ping(client)??throw new UnableToConnect();
            Log(LogLevel.Debug, "Established connection to [Host:{}, Version:{}, StartTime:{}, UpTime:{}]",
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
        {
            return connectionOptions.Logger?.CreateLogger($"KubeMQContract[Conn({this.id}),Sub({subID})]");
        }

        public void Dispose()
        {
            dataLock.EnterWriteLock();
            foreach (var sub in subscriptions)
            {
                sub.Stop();
            }
            subscriptions.Clear();
            client.Dispose();
            dataLock.ExitWriteLock();
            dataLock.Dispose();
        }
    }
}
