using Google.Protobuf.Collections;
using Grpc.Core;
using KubeMQ.Contract.Attributes;
using KubeMQ.Contract.Interfaces;
using KubeMQ.Contract.Interfaces.Messages;
using KubeMQ.Contract.SDK;
using KubeMQ.Contract.SDK.Grpc;
using System.Reflection;

namespace KubeMQ.Contract.Subscriptions
{
    internal class RPCCommandSubscription<T> : SubscriptionBase<Request>
    {
        private readonly IMessageFactory<T> incomingFactory;
        private readonly KubeSubscription<T> subscription;
        private readonly Func<IMessage<T>, TaggedResponse<bool>> processMessage;
        
        public RPCCommandSubscription(IMessageFactory<T> incomingFactory, KubeSubscription<T> subscription, kubemq.kubemqClient client, ConnectionOptions connectionOptions, Func<IMessage<T>, TaggedResponse<bool>> processMessage, Action<Exception> errorRecieved, ILogProvider logProvider, CancellationToken cancellationToken)
            : base(client, connectionOptions, errorRecieved, logProvider, cancellationToken)
        {
            this.incomingFactory=incomingFactory;
            this.subscription = subscription;
            this.processMessage = processMessage;
        }

        protected override AsyncServerStreamingCall<Request> EstablishCall()
        {
            logProvider.LogTrace("Attempting to establish RPC Command subscription {} to {} on channel {} for type {}", ID, options.Address, subscription.Channel, Utility.TypeName<T>());
            return client.SubscribeToRequests(new Subscribe()
            {
                Channel = subscription.Channel,
                ClientID = subscription.ClientID,
                Group = subscription.Group,
                SubscribeTypeData = Subscribe.Types.SubscribeType.Commands
            },
            options.GrpcMetadata,
            null, cancellationToken.Token);
        }

        protected override void ProcessEvent(Request evnt)
        {
            Task.Run(() =>
            {
                logProvider.LogTrace("Message recieved {} on RPC subscription {}", evnt.RequestID, ID);
                var msg = incomingFactory.ConvertMessage(logProvider, evnt);
                if (msg==null)
                    throw new NullReferenceException(nameof(msg));
                var result = processMessage(msg);
                logProvider.LogTrace("Response generated for {} on RPC subscription {}", evnt.RequestID, ID);
                var tags = new MapField<string, string>();
                if (result.Tags!=null)
                {
                    foreach (var tag in result.Tags)
                        tags.Add(tag.Key, tag.Value);
                }
                client.SendResponse(new Response()
                {
                    CacheHit=false,
                    RequestID= evnt.RequestID,
                    ClientID=subscription.ClientID,
                    Executed=result.Response,
                    Error=string.Empty,
                    ReplyChannel=evnt.ReplyChannel,
                    Body=Google.Protobuf.ByteString.Empty,
                    Metadata=string.Empty,
                    Tags = { tags },
                    Timestamp=Utility.ToUnixTime(DateTime.Now)
                }, headers: options.GrpcMetadata, deadline: null, cancellationToken: cancellationToken.Token);
            }, cancellationToken.Token)
            .ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    Exception ex = t.Exception!;
                    while (ex is AggregateException && ex.InnerException != null)
                        ex = ex.InnerException;
                    logProvider.LogError("Message {} failed on subscription {}.  Message:{}", evnt.RequestID, ID, ex.Message);
                    errorRecieved(ex);
                    client.SendResponse(new Response()
                    {
                        RequestID=evnt.RequestID,
                        ClientID=subscription.ClientID,
                        Executed=false,
                        Error=ex.Message,
                        ReplyChannel=evnt.ReplyChannel,
                        Body=Google.Protobuf.ByteString.Empty,
                        Timestamp=Utility.ToUnixTime(DateTime.Now)
                    }, headers: options.GrpcMetadata, deadline: null, cancellationToken: cancellationToken.Token);
                }
            });
        }
    }
}
