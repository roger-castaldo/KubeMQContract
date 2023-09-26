using KubeMQ.Contract.SDK.Grpc;
using KubeMQ.Contract.SDK.Interfaces;
using Microsoft.Extensions.Logging;

namespace KubeMQ.Contract.Interfaces.Messages
{
    internal interface IMessageFactory<T> 
    {
        IKubeEnqueue Enqueue(T message, ConnectionOptions connectionOptions,string clientID, string? channel, Dictionary<string, string>? tagCollection, int? delaySeconds, int? expirationSeconds, int? maxCount, string? maxCountChannel);
        IKubeBatchEnqueue Enqueue(IEnumerable<T> messages, ConnectionOptions connectionOptions, string clientID, string? channel, Dictionary<string, string>? tagCollection, int? delaySeconds, int? expirationSeconds, int? maxCount, string? maxCountChannel);
        IKubeEvent Event(T message, ConnectionOptions connectionOptions, string clientID, string? channel, Dictionary<string, string>? tagCollection);
        IKubeRequest Request(T message, ConnectionOptions connectionOptions, string clientID, string? channel, Dictionary<string, string>? tagCollection, int? timeout, Request.Types.RequestType type);
        IKubeMessage Response(T message, ConnectionOptions connectionOptions, string clientID, string responseChannel, Dictionary<string, string>? tagCollection);
        IInternalMessage<T> ConvertMessage(ILogger? logger, QueueMessage msg);
        IInternalMessage<T> ConvertMessage(ILogger? logger, SRecievedMessage<Request> msg);
        IInternalMessage<T> ConvertMessage(ILogger? logger, SRecievedMessage<EventReceive> msg);
        IResultMessage<T> ConvertMessage(ILogger? logger, Response msg);
        bool CanConvertFrom(Type responseType);
    }
}
