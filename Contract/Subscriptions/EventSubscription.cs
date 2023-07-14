using Grpc.Core;
using KubeMQ.Contract.Attributes;
using KubeMQ.Contract.Interfaces;
using KubeMQ.Contract.Interfaces.Messages;
using KubeMQ.Contract.SDK;
using KubeMQ.Contract.SDK.Connection;
using KubeMQ.Contract.SDK.Grpc;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace KubeMQ.Contract.Subscriptions
{
    internal class EventSubscription<T> : SubscriptionBase<EventReceive> 
    {
        private readonly IMessageFactory<T> messageFactory;
        private readonly KubeSubscription<T> subscription;
        private readonly Func<IMessage<T>,Task> messageRecieved;
        private readonly long storageOffset;
        private readonly Subscribe.Types.EventsStoreType eventsStoreStyle;
        private readonly Subscribe.Types.SubscribeType eventType;

        public EventSubscription(Guid id,IMessageFactory<T> messageFactory, KubeSubscription<T> subscription, KubeClient client, ConnectionOptions options, Func<IMessage<T>, Task> messageRecieved, Action<Exception> errorRecieved, long storageOffset, ILogger? logger, MessageReadStyle? messageReadStyle,CancellationToken cancellationToken)
            : base(id,client,options,errorRecieved,logger,cancellationToken)
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
            logger?.LogTrace("Attempting to establish subscription {} to {} on channel {} for type {}", ID, options.Address, subscription.Channel, Utility.TypeName<T>());
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
            cancellationToken.Token);
        }

        protected override void EstablishReader()
        {
            Task.Run(async () =>
            {
                while (await channel.Reader.WaitToReadAsync(cancellationToken.Token))
                {
                    while (channel.Reader.TryRead(out SRecievedMessage<EventReceive> message))
                    {
                        logger?.LogTrace("Message recieved {} on subscription {}", message.Data.EventID, ID);
                        var msg = messageFactory.ConvertMessage(logger, message);
                        if (msg.Exception!=null)
                            throw msg.Exception;
                        try
                        {
                            await messageRecieved(msg);
                        }
                        catch (Exception ex)
                        {
                            logger?.LogError("Message {} failed on subscription {}.  Message:{}", message.Data.EventID, ID, ex.Message);
                            errorRecieved(ex);
                        }
                    }
                }
            });
        }
    }
}
