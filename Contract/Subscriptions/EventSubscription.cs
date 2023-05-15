using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using KubeMQ.Contract.Attributes;
using KubeMQ.Contract.Interfaces;
using KubeMQ.Contract.SDK;
using KubeMQ.Contract.SDK.Grpc;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace KubeMQ.Contract.Subscriptions
{
    internal class EventSubscription<T> : IMessageSubscription
    {
        public Guid ID => Guid.NewGuid();
        private readonly IMessageFactory<T> messageFactory;
        private readonly KubeSubscription<T> subscription;
        private readonly kubemq.kubemqClient client;
        private readonly ConnectionOptions options;
        private readonly Action<IMessage<T>> messageRecieved;
        private readonly Action<Exception> errorRecieved;
        private readonly CancellationTokenSource cancellationToken;
        private readonly long storageOffset;
        private readonly Subscribe.Types.EventsStoreType eventsStoreStyle;
        private bool active = true;
        private readonly ILogProvider logProvider;

        public EventSubscription(IMessageFactory<T> messageFactory, KubeSubscription<T> subscription, kubemq.kubemqClient client, ConnectionOptions options, Action<IMessage<T>> messageRecieved, Action<Exception> errorRecieved, long storageOffset, ILogProvider logProvider, MessageReadStyle? messageReadStyle, CancellationToken cancellationToken)
        {
            messageReadStyle ??= typeof(T).GetCustomAttributes<StoredMessage>().Select(sm => sm.Style).FirstOrDefault();
            this.messageFactory=messageFactory;
            this.subscription = subscription;
            this.client = client;
            this.options = options;
            this.messageRecieved = messageRecieved;
            this.errorRecieved = errorRecieved;
            this.storageOffset = storageOffset;
            this.logProvider=logProvider;
            this.eventsStoreStyle=(messageReadStyle==null ? Subscribe.Types.EventsStoreType.Undefined : (Subscribe.Types.EventsStoreType)(int)messageReadStyle);
            this.cancellationToken = new CancellationTokenSource();

            cancellationToken.Register(() =>
            {
                active = false;
                this.cancellationToken.Cancel();
            });

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            Start();
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        }

        private async Task Start()
        {
            var eventType = Subscribe.Types.SubscribeType.Events;
            if (typeof(T).GetCustomAttributes<StoredMessage>().Any())
                eventType = Subscribe.Types.SubscribeType.EventsStore;
            logProvider.LogTrace("Attempting to establish subscription {} to {} on channel {} for type {}",ID, options.Address, subscription.Channel, typeof(T).Name);
            while (active && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    using var call = client.SubscribeToEvents(new Subscribe()
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
                    logProvider.LogTrace("Connection for subscription {} established", ID);
                    while (active && await call.ResponseStream.MoveNext(cancellationToken.Token))
                    {
                        if (active)
                        {
                            var evnt = call.ResponseStream.Current;
                            try
                            {
                                logProvider.LogTrace("Message recieved {} on subscription {}", evnt.EventID, ID);
                                var msg = messageFactory.ConvertMessage(logProvider, evnt);
                                messageRecieved(msg);
                            }
                            catch (Exception e)
                            {
                                logProvider.LogError("Message {} failed on subscription {}.  Message:{}", evnt.EventID, ID, e.Message);
                                errorRecieved(e);
                            }
                        }
                    }
                }
                catch (RpcException rpcx)
                {
                    if (rpcx.StatusCode == StatusCode.Cancelled)
                    {
                        break;
                    }
                    else
                    {
                        logProvider.LogError("RPC Error recieved on subscription {}.  StatusCode:{},Message:{}", ID, rpcx.StatusCode, rpcx.Message);
                        errorRecieved(rpcx);
                    }
                }
                catch (Exception e)
                {
                    logProvider.LogError("Error recieved on subscription {}.  Message:{}", ID, e.Message);
                    errorRecieved(e);
                }

                await Task.Delay(options.ReconnectInterval);
            }
        }

        public void Stop()
        {
            logProvider.LogTrace("Stop called for subscription {}", ID);
            active = false;
        }
    }
}
