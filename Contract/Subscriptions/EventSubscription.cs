using Grpc.Core;
using KubeMQ.Contract.Attributes;
using KubeMQ.Contract.Interfaces;
using KubeMQ.Contract.Interfaces.Messages;
using KubeMQ.Contract.SDK;
using KubeMQ.Contract.SDK.Grpc;
using System.Reflection;

namespace KubeMQ.Contract.Subscriptions
{
    internal class EventSubscription<T> : SubscriptionBase<EventReceive>
    {
        private readonly IMessageFactory<T> messageFactory;
        private readonly KubeSubscription<T> subscription;
        private readonly Action<IMessage<T>> messageRecieved;
        private readonly long storageOffset;
        private readonly Subscribe.Types.EventsStoreType eventsStoreStyle;
        private readonly Subscribe.Types.SubscribeType eventType;

        public EventSubscription(IMessageFactory<T> messageFactory, KubeSubscription<T> subscription, kubemq.kubemqClient client, ConnectionOptions options, Action<IMessage<T>> messageRecieved, Action<Exception> errorRecieved, long storageOffset, ILogProvider logProvider, MessageReadStyle? messageReadStyle, CancellationToken cancellationToken)
            : base(client,options,errorRecieved,logProvider,cancellationToken)
        {
            messageReadStyle ??= typeof(T).GetCustomAttributes<StoredMessage>().Select(sm => sm.Style).FirstOrDefault();
            this.messageFactory=messageFactory;
            this.subscription = subscription;
            this.messageRecieved = messageRecieved;
            this.storageOffset = storageOffset;
            this.eventsStoreStyle=(messageReadStyle==null ? Subscribe.Types.EventsStoreType.Undefined : (Subscribe.Types.EventsStoreType)(int)messageReadStyle);
            eventType = Subscribe.Types.SubscribeType.Events;
            if (typeof(T).GetCustomAttributes<StoredMessage>().Any())
                eventType = Subscribe.Types.SubscribeType.EventsStore;
        }

        protected override AsyncServerStreamingCall<EventReceive> EstablishCall()
        {
            logProvider.LogTrace("Attempting to establish subscription {} to {} on channel {} for type {}", ID, options.Address, subscription.Channel, typeof(T).Name);
            return client.SubscribeToEvents(new Subscribe()
            {
                Channel = subscription.Channel,
                ClientID = subscription.ClientID,
                Group = subscription.Group,
                SubscribeTypeData = eventType,
                EventsStoreTypeData = eventsStoreStyle,
                EventsStoreTypeValue = storageOffset
            },
            options.GrpcMetadata,
            null, cancellationToken.Token);
        }

        protected override void ProcessEvent(EventReceive evnt)
        {
            Task.Run(() =>
            {
                logProvider.LogTrace("Message recieved {} on subscription {}", evnt.EventID, ID);
                var msg = messageFactory.ConvertMessage(logProvider, evnt);
                messageRecieved(msg);
            }, cancellationToken.Token)
            .ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    Exception ex = t.Exception!;
                    while (ex is AggregateException && ex.InnerException != null)
                        ex = ex.InnerException;
                    logProvider.LogError("Message {} failed on subscription {}.  Message:{}", evnt.EventID, ID, ex.Message);
                    errorRecieved(ex);
                }
            });
        }

    }
}
