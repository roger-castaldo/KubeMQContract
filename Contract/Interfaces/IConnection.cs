using Google.Protobuf;
using KubeMQ.Contract.SDK.Grpc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KubeMQ.Contract.Interfaces
{
    public interface IConnection
    {
        IPingResult Ping();
        Task<ITransmissionResult> Send<T>(T message, CancellationToken cancellationToken = new CancellationToken(), string? channel = null);
        Task<IMessage<R>> SendRPC<T, R>(T message, CancellationToken cancellationToken = new CancellationToken(), string? channel = null,int? timeout=null, RPCType? type=null);
        Task<ITransmissionResult> EnqueueMessage<T>(T message, CancellationToken cancellationToken = new CancellationToken(), string? channel = null,int? expirationSeconds = null,int? delaySeconds=null,int? maxQueueSize=null,string? maxQueueChannel=null);
        Task<IBatchTransmissionResult> EnqueueMessages<T>(IEnumerable<T> messages, CancellationToken cancellationToken = new CancellationToken(), string? channel = null, int? expirationSeconds = null, int? delaySeconds = null, int? maxQueueSize = null, string? maxQueueChannel = null);
        Guid Subscribe<T>(
            Action<T> messageRecieved, 
            Action<string> errorRecieved, 
            CancellationToken cancellationToken = new CancellationToken(), 
            string? channel = null, 
            string group = "",
            long storageOffset=0);

        Guid SubscribeRPC<T, R>(
            Func<T,R> processMessage,
            Action<string> errorRecieved,
            CancellationToken cancellationToken = new CancellationToken(),
            string? channel = null,
            string group = "",
            RPCType? commandType=null
        );

        IMessageQueue<T> SubscribeToQueue<T>(
            CancellationToken cancellationToken = new CancellationToken(),
            string? channel = null
        );

        void Unsubscribe(Guid id);
    }
}
