using Grpc.Core;
using KubeMQ.Contract.Interfaces.Messages;
using KubeMQ.Contract.SDK;
using KubeMQ.Contract.SDK.Connection;
using KubeMQ.Contract.SDK.Grpc;
using Microsoft.Extensions.Logging;

namespace KubeMQ.Contract.Subscriptions
{
    internal class RPCQuerySubscription<T,R> : SubscriptionBase<Request> 
    {
        private readonly IMessageFactory<T> incomingFactory;
        private readonly IMessageFactory<R> outgoingFactory;
        private readonly KubeSubscription<T> subscription;
        private readonly Func<Contract.Interfaces.Messages.IMessage<T>, Task<TaggedResponse<R>>> processMessage;
        
        public RPCQuerySubscription(Guid id,IMessageFactory<T> incomingFactory, IMessageFactory<R> outgoingFactory, KubeSubscription<T> subscription, KubeClient client, ConnectionOptions connectionOptions, Func<Contract.Interfaces.Messages.IMessage<T>, Task<TaggedResponse<R>>> processMessage, Action<Exception> errorRecieved, ILogger? logger,bool synchronous, CancellationToken cancellationToken)
            : base(id,client, connectionOptions, errorRecieved, logger,synchronous, cancellationToken)
        {
            this.incomingFactory=incomingFactory;
            this.outgoingFactory=outgoingFactory;
            this.subscription = subscription;
            this.processMessage = processMessage;
        }

        protected override AsyncServerStreamingCall<Request> EstablishCall()
        {
            logger?.LogTrace("Attempting to establish RPC Query subscription {SubscriptionID} to {Address} on channel {Channel} for type {RequestType} returning type {ResponseType}", ID, options.Address, subscription.Channel, Utility.TypeName<T>(), Utility.TypeName<R>());
            return client.SubscribeToRequests(new Subscribe()
            {
                Channel = subscription.Channel,
                ClientID = subscription.ClientID,
                Group = subscription.Group,
                SubscribeTypeData = Subscribe.Types.SubscribeType.Queries
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
            TaggedResponse<R>? result = null;
            try
            {
                result = await processMessage(msg);
            }
            catch (Exception ex)
            {
                logger?.LogError("Message {MessageID} failed on subscription {SubscriptionID}.  Message:{ErrorMessage}", message.Data.RequestID, ID, ex.Message);
                await client.SendResponseAsync(new Response()
                {
                    RequestID=message.Data.RequestID,
                    ClientID=subscription.ClientID,
                    Executed=true,
                    Error=ex.Message,
                    ReplyChannel=message.Data.ReplyChannel,
                    Body=Google.Protobuf.ByteString.Empty,
                    Timestamp=Utility.ToUnixTime(DateTime.Now)
                },
                options.GrpcMetadata,
                cancellationToken.Token);
                throw;
            }
            if (result!=null)
            {
                var response = outgoingFactory.Response(result.Response, options, subscription.ClientID, message.Data.ReplyChannel, result.Tags);
                logger?.LogTrace("Response generated for {MessageID} on RPC subscription {SubscriptionID}", message.Data.RequestID, ID);
                try
                {
                    await client.SendResponseAsync(new Response()
                    {
                        CacheHit=false,
                        RequestID= message.Data.RequestID,
                        ClientID=message.Data.ClientID,
                        Executed=true,
                        Error=string.Empty,
                        ReplyChannel=response.Channel,
                        Body=Google.Protobuf.ByteString.CopyFrom(response.Body),
                        Metadata=response.MetaData,
                        Tags = { response.Tags },
                        Timestamp=Utility.ToUnixTime(DateTime.Now)
                    },
                    options.GrpcMetadata,
                    cancellationToken.Token);
                }
                catch (Exception e)
                {
                    logger?.LogError("Response for {MessageID} on RPC subscription {SubscriptionID} failed to send. Exception: {ErrorMessage}", message.Data.RequestID, ID,e.Message);
                    throw;
                }
            }
        }
    }
}
