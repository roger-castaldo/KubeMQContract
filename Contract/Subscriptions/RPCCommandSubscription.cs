using Google.Protobuf.Collections;
using Grpc.Core;
using KubeMQ.Contract.Interfaces.Messages;
using KubeMQ.Contract.SDK;
using KubeMQ.Contract.SDK.Connection;
using KubeMQ.Contract.SDK.Grpc;
using Microsoft.Extensions.Logging;

namespace KubeMQ.Contract.Subscriptions
{
    internal class RPCCommandSubscription<T> : SubscriptionBase<Request>
    {
        private readonly IMessageFactory<T> incomingFactory;
        private readonly KubeSubscription<T> subscription;
        private readonly Func<Contract.Interfaces.Messages.IMessage<T>, Task<TaggedResponse<bool>>> processMessage;
        
        public RPCCommandSubscription(Guid id,IMessageFactory<T> incomingFactory, KubeSubscription<T> subscription, KubeClient client, ConnectionOptions connectionOptions, Func<Contract.Interfaces.Messages.IMessage<T>, Task<TaggedResponse<bool>>> processMessage, Action<Exception> errorRecieved, ILogger? logger,bool synchronous, CancellationToken cancellationToken)
            : base(id,client, connectionOptions, errorRecieved, logger,synchronous, cancellationToken)
        {
            this.incomingFactory=incomingFactory;
            this.subscription = subscription;
            this.processMessage = processMessage;
        }

        protected override AsyncServerStreamingCall<Request> EstablishCall()
        {
            logger?.LogTrace("Attempting to establish RPC Command subscription {SubscriptionID} to {Address} on channel {Channel} for type {MessageType}", ID, options.Address, subscription.Channel, Utility.TypeName<T>());
            return client.SubscribeToRequests(new Subscribe()
            {
                Channel = subscription.Channel,
                ClientID = subscription.ClientID,
                Group = subscription.Group,
                SubscribeTypeData = Subscribe.Types.SubscribeType.Commands
            },
            options.GrpcMetadata,
            cancellationToken.Token);
        }

        protected override async Task ProcessMessage(SRecievedMessage<Request> message)
        {
            logger?.LogTrace("Message recieved {MessageID} on RPC subscription {SubscriptionID}", message.Data.RequestID, ID);
            var msg = incomingFactory.ConvertMessage(logger, message);
            if (msg==null)
                throw new NullReferenceException(nameof(msg));
            else if (msg.Exception!=null)
                throw msg.Exception;
            TaggedResponse<bool>? result = null;
            try
            {
                result = await processMessage(msg);
            }
            catch (Exception e)
            {
                logger?.LogError("Message {MessageID} failed on subscription {SubscriptionID}.  Message:{ErrorMessage}", message.Data.RequestID, ID, e.Message);
                errorRecieved(e);
                try
                {
                    await client.SendResponseAsync(new Response()
                    {
                        RequestID=message.Data.RequestID,
                        ClientID=subscription.ClientID,
                        Executed=false,
                        Error=e.Message,
                        ReplyChannel=message.Data.ReplyChannel,
                        Body=Google.Protobuf.ByteString.Empty,
                        Timestamp=Utility.ToUnixTime(DateTime.Now)
                    },
                    options.GrpcMetadata,
                    cancellationToken.Token);
                }
                catch (Exception ex)
                {
                    errorRecieved(ex);
                }
            }
            if (result!=null)
            {
                logger?.LogTrace("Response generated for {MessageID} on RPC subscription {SubscriptionID}", message.Data.RequestID, ID);
                var tags = new MapField<string, string>();
                if (result.Tags!=null)
                {
                    foreach (var tag in result.Tags)
                        tags.Add(tag.Key, tag.Value);
                }
                await client.SendResponseAsync(new Response()
                {
                    CacheHit=false,
                    RequestID= message.Data.RequestID,
                    ClientID=subscription.ClientID,
                    Executed=result.Response,
                    Error=string.Empty,
                    ReplyChannel=message.Data.ReplyChannel,
                    Body=Google.Protobuf.ByteString.Empty,
                    Metadata=string.Empty,
                    Tags = { tags },
                    Timestamp=Utility.ToUnixTime(DateTime.Now)
                },
                options.GrpcMetadata,
                cancellationToken.Token);
            }
        }
    }
}
