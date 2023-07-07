using Grpc.Core;
using KubeMQ.Contract.Attributes;
using KubeMQ.Contract.Interfaces;
using KubeMQ.Contract.Interfaces.Messages;
using KubeMQ.Contract.SDK;
using KubeMQ.Contract.SDK.Grpc;
using System.Reflection;

namespace KubeMQ.Contract.Subscriptions
{
    internal class RPCQuerySubscription<T,R> : SubscriptionBase<Request>
    {
        private readonly IMessageFactory<T> incomingFactory;
        private readonly IMessageFactory<R> outgoingFactory;
        private readonly KubeSubscription<T> subscription;
        private readonly Func<IMessage<T>, TaggedResponse<R>> processMessage;
        
        public RPCQuerySubscription(IMessageFactory<T> incomingFactory, IMessageFactory<R> outgoingFactory, KubeSubscription<T> subscription, kubemq.kubemqClient client, ConnectionOptions connectionOptions, Func<IMessage<T>, TaggedResponse<R>> processMessage, Action<Exception> errorRecieved, ILogProvider logProvider, CancellationToken cancellationToken)
            : base(client, connectionOptions, errorRecieved, logProvider, cancellationToken)
        {
            this.incomingFactory=incomingFactory;
            this.outgoingFactory=outgoingFactory;
            this.subscription = subscription;
            this.processMessage = processMessage;
        }

        protected override AsyncServerStreamingCall<Request> EstablishCall()
        {
            logProvider.LogTrace("Attempting to establish RPC Query subscription {} to {} on channel {} for type {} returning type {}", ID, options.Address, subscription.Channel, Utility.TypeName<T>(), Utility.TypeName<R>());
            return client.SubscribeToRequests(new Subscribe()
            {
                Channel = subscription.Channel,
                ClientID = subscription.ClientID,
                Group = subscription.Group,
                SubscribeTypeData = Subscribe.Types.SubscribeType.Queries
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
                if (result==null)
                    throw new NullReferenceException(nameof(result));
                var response = outgoingFactory.Response(result.Response, options, evnt.ReplyChannel, result.Tags);
                logProvider.LogTrace("Response generated for {} on RPC subscription {}", evnt.RequestID, ID);
                client.SendResponse(new Response()
                {
                    CacheHit=false,
                    RequestID= evnt.RequestID,
                    ClientID=subscription.ClientID,
                    Executed=true,
                    Error=string.Empty,
                    ReplyChannel=response.Channel,
                    Body=Google.Protobuf.ByteString.CopyFrom(response.Body),
                    Metadata=response.MetaData,
                    Tags = { response.Tags },
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
                        Executed=true,
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
