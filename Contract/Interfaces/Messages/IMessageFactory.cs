using KubeMQ.Contract.SDK.Grpc;
using KubeMQ.Contract.SDK.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KubeMQ.Contract.Interfaces.Messages
{
    internal interface IMessageFactory<T>
    {
        IKubeEnqueue Enqueue(T message, ConnectionOptions connectionOptions, string? channel, Dictionary<string, string>? tagCollection, int? delaySeconds, int? expirationSeconds, int? maxCount, string? maxCountChannel);
        IKubeBatchEnqueue Enqueue(IEnumerable<T> messages, ConnectionOptions connectionOptions, string? channel, Dictionary<string, string>? tagCollection, int? delaySeconds, int? expirationSeconds, int? maxCount, string? maxCountChannel);
        IKubeEvent Event(T message, ConnectionOptions connectionOptions, string? channel, Dictionary<string, string>? tagCollection);
        IKubeRequest Request(T message, ConnectionOptions connectionOptions, string? channel, Dictionary<string, string>? tagCollection, int? timeout, Request.Types.RequestType type);
        IKubeMessage Response(T message, ConnectionOptions connectionOptions, string responseChannel, Dictionary<string, string>? tagCollection);
        IMessage<T> ConvertMessage(ILogProvider logProvider, QueueMessage msg);
        IMessage<T> ConvertMessage(ILogProvider logProvider, Request msg);
        IMessage<T> ConvertMessage(ILogProvider logProvider, EventReceive msg);
        IResultMessage<T> ConvertMessage(ILogProvider logProvider, Response msg);
    }
}
