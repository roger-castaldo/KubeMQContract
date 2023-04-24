using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using KubeMQ.Contract.Interfaces;
using KubeMQ.Contract.SDK;
using KubeMQ.Contract.SDK.Grpc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KubeMQ.Contract
{
    internal class EventSubscription<T> : IMessageSubscription
    {
        public Guid ID => Guid.NewGuid();
        private readonly KubeSubscription subscription;
        private readonly kubemq.kubemqClient client;
        private readonly ConnectionOptions options;
        private readonly Action<T> messageRecieved;
        private readonly Action<string> errorRecieved;
        private readonly CancellationTokenSource cancellationToken;
        private bool active=true;

        public EventSubscription(KubeSubscription subscription,kubemq.kubemqClient client, ConnectionOptions options, Action<T> messageRecieved, Action<string> errorRecieved, CancellationToken cancellationToken)
        {
            this.subscription=subscription;
            this.client=client;
            this.options=options;
            this.messageRecieved=messageRecieved;
            this.errorRecieved=errorRecieved;
            this.cancellationToken = new CancellationTokenSource();

            cancellationToken.Register(() => {
                this.active=false;
                this.cancellationToken.Cancel();
            });

            start();
        }

        private async Task start()
        {
            while (active && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    using (var call = client.SubscribeToEvents(new Subscribe()
                        {
                            Channel=subscription.Channel,
                            ClientID=subscription.ClientID,
                            Group=subscription.Group,
                            SubscribeTypeData=Subscribe.Types.SubscribeType.Events
                        },
                        options.GrpcMetadata,
                        null, this.cancellationToken.Token))
                    {
                        while (active && await call.ResponseStream.MoveNext(this.cancellationToken.Token))
                        {
                            if (active)
                            {
                                var msg = Utility.ConvertMessage<T>(call.ResponseStream.Current);
                                messageRecieved(msg);
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
                        errorRecieved(rpcx.Message);
                    }
                }
                catch (Exception e)
                {
                    errorRecieved(e.Message);
                }

                await Task.Delay(options.ReconnectInterval);
            }
        }

        public void Stop()
        {
            active=false;
        }
    }
}
