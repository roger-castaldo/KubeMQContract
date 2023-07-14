using Grpc.Core;
using KubeMQ.Contract.Interfaces;
using KubeMQ.Contract.Interfaces.Messages;
using KubeMQ.Contract.SDK.Connection;
using KubeMQ.Contract.SDK.Grpc;
using Microsoft.Extensions.Logging;
using System.Threading.Channels;

namespace KubeMQ.Contract.Subscriptions
{
    internal abstract class SubscriptionBase<TResponse> : IMessageSubscription
    {
        public Guid ID { get; private init; } = Guid.NewGuid();
        protected readonly KubeClient client;
        protected readonly ConnectionOptions options;
        protected readonly CancellationTokenSource cancellationToken;
        private bool active = true;
        private ManualResetEvent? startEvent;
        private bool disposedValue;
        protected readonly ILogger? logger;
        protected readonly Action<Exception> errorRecieved;
        protected readonly Channel<SRecievedMessage<TResponse>> channel;

        public SubscriptionBase(Guid id,KubeClient client, ConnectionOptions options, Action<Exception> errorRecieved, ILogger? logger, CancellationToken cancellationToken)
        {
            this.ID=id;
            this.client = client;
            this.options = options;
            this.errorRecieved = errorRecieved;
            this.logger=logger;
            this.cancellationToken = new CancellationTokenSource();
            channel = Channel.CreateUnbounded<SRecievedMessage<TResponse>>(new UnboundedChannelOptions()
            {
                SingleReader=true,
                SingleWriter=true
            });

            cancellationToken.Register(() =>
            {
                this.cancellationToken.Cancel();
            });

            this.cancellationToken.Token.Register(() =>
            {
                active = false;
                client.Dispose();
            });
        }


        public void Start()
        {
            startEvent = new ManualResetEvent(false);
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            Run();
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            startEvent.WaitOne();
            startEvent.Dispose();
            startEvent = null;
            EstablishReader();
        }

        private void Run()
        {

            Task.Run(async () =>
            {
                while (active && !cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        using var call = EstablishCall();
                        startEvent?.Set();
                        logger?.LogTrace("Connection for subscription {} established", ID);
                        await foreach (var resp in call.ResponseStream.ReadAllAsync(cancellationToken.Token))
                        {
                            if (active)
                                await channel.Writer.WriteAsync(new SRecievedMessage<TResponse>(resp));
                            else
                                break;
                        }
                        call.Dispose();
                    }
                    catch (RpcException rpcx)
                    {
                        switch (rpcx.StatusCode)
                        {
                            case StatusCode.Cancelled:
                            case StatusCode.PermissionDenied:
                            case StatusCode.Aborted:
                                Stop();
                                break;
                            case StatusCode.Unknown:
                            case StatusCode.Unavailable:
                            case StatusCode.DataLoss:
                            case StatusCode.DeadlineExceeded:
                                logger?.LogTrace("RPC Error recieved on subscription {}, retrying connection after delay {}ms.  StatusCode:{},Message:{}", ID, options.ReconnectInterval, rpcx.StatusCode, rpcx.Message);
                                break;
                            default:
                                logger?.LogError("RPC Error recieved on subscription {}.  StatusCode:{},Message:{}", ID, rpcx.StatusCode, rpcx.Message);
                                errorRecieved(rpcx);
                                break;
                        }
                    }
                    catch (Exception e)
                    {
                        logger?.LogError("Error recieved on subscription {}.  Message:{}", ID, e.Message);
                        errorRecieved(e);
                    }
                    if (active && !cancellationToken.IsCancellationRequested)
                        await Task.Delay(options.ReconnectInterval);
                }
            });
        }

        public void Stop()
        {
            logger?.LogTrace("Stop called for subscription {}", ID);
            active = false;
            this.cancellationToken.Cancel();
        }

        protected abstract AsyncServerStreamingCall<TResponse> EstablishCall();
        protected abstract void EstablishReader();

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                    if (!active)
                        Stop();
                    channel.Writer.Complete();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue=true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~SubscriptionBase()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
