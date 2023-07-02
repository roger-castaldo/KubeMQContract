using Grpc.Core;
using KubeMQ.Contract.Interfaces;
using KubeMQ.Contract.Interfaces.Messages;
using KubeMQ.Contract.SDK;
using KubeMQ.Contract.SDK.Grpc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static KubeMQ.Contract.SDK.Grpc.Subscribe.Types;

namespace KubeMQ.Contract.Subscriptions
{
    internal abstract class SubscriptionBase<TResponse> : IMessageSubscription
    {
        public Guid ID { get; private init; } = Guid.NewGuid();
        protected readonly kubemq.kubemqClient client;
        protected readonly ConnectionOptions options;
        protected readonly CancellationTokenSource cancellationToken;
        private bool active = true;
        protected readonly ILogProvider logProvider;
        protected readonly Action<Exception> errorRecieved;

        public SubscriptionBase(kubemq.kubemqClient client, ConnectionOptions options, Action<Exception> errorRecieved,ILogProvider logProvider, CancellationToken cancellationToken)
        {
            this.client = client;
            this.options = options;
            this.errorRecieved = errorRecieved;
            this.logProvider=logProvider;
            this.cancellationToken = new CancellationTokenSource();

            cancellationToken.Register(() =>
            {
                active = false;
                this.cancellationToken.Cancel();
            });
        }


        public async Task Start()
        {
            while (active && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    using var call = EstablishCall(); 
                    logProvider.LogTrace("Connection for subscription {} established", ID);
                    while (active && await call.ResponseStream.MoveNext(cancellationToken.Token))
                    {
                        if (active)
                            ProcessEvent(call.ResponseStream.Current);
                        else
                            break;
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
                if (active && !cancellationToken.IsCancellationRequested)
                    await Task.Delay(options.ReconnectInterval);
            }
        }

        public void Stop()
        {
            logProvider.LogTrace("Stop called for subscription {}", ID);
            active = false;
            this.cancellationToken.Cancel();
        }

        protected abstract AsyncServerStreamingCall<TResponse> EstablishCall();
        protected abstract void ProcessEvent(TResponse evnt);
    }
}
