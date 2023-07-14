using Grpc.Core;
using Grpc.Net.Client;
using KubeMQ.Contract.Interfaces;
using KubeMQ.Contract.SDK.Grpc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace KubeMQ.Contract.SDK.Connection
{
    internal class KubeClient : IDisposable
    {
        private const int RETRY_COUNT = 5;

        private readonly string address;
        private readonly ChannelCredentials credentials;
        private readonly ILogger? logger;
        private readonly AutoResetEvent dataLock;
        private GrpcChannel channel;
        private kubemq.kubemqClient client;
        private bool disposedValue;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public KubeClient(string address, ChannelCredentials credentials, ILogger? logger)
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        {
            this.address=address;
            this.credentials = credentials;
            this.logger = logger;
            dataLock = new AutoResetEvent(true);
            ProduceClient();
        }

        private void ProduceClient()
        {
            channel = GrpcChannel.ForAddress(address, new GrpcChannelOptions()
            {
                Credentials=credentials,
                HttpHandler = new SocketsHttpHandler
                {
                    KeepAlivePingDelay = TimeSpan.FromSeconds(60),
                    KeepAlivePingTimeout = TimeSpan.FromSeconds(30),
                    KeepAlivePingPolicy = HttpKeepAlivePingPolicy.Always,
                    EnableMultipleHttp2Connections = true
                }
            });
            client = new(channel);
        }

        private void CheckDisposed()
        {
            if (disposedValue)
                throw new NullReferenceException("Client has already been disposed");
        }

        private async Task<R> TryInvoke<R>(Func<Task<R>> call)
        {
            CheckDisposed();
            var errCount = 0;
            Exception? err = null;
            R? result = default;
            dataLock.WaitOne();
            while (errCount<RETRY_COUNT)
            {
                try
                {
                    result = await call();
                    break;
                }
                catch (RpcException ex)
                {
                    err=ex;
                    logger?.LogError("Error occured on Send in send Message:{}, Status: {}", ex.Message, ex.Status);
                    if (ex.StatusCode==StatusCode.Unavailable)
                    {
                        errCount++;
                        try { channel.Dispose(); } catch (Exception) { }
                        try { ProduceClient(); } catch (Exception) { break; }
                    }
                    else
                        break;
                }catch(Exception ex)
                {
                    err=ex;
                    break;
                }
            }
            dataLock.Set();
            if (err!=null)
                throw err;
            return result!;
        }

        internal KubeMQ.Contract.SDK.Grpc.PingResult? Ping()
        {
            var tsk = TryInvoke<KubeMQ.Contract.SDK.Grpc.PingResult>(() =>
            {
                return Task.FromResult(client.Ping(new Empty()));
            });
            tsk.Wait();
            if (tsk.Exception!=null)
                return null;
            return tsk.Result;
        }

        internal async Task<KubeMQ.Contract.SDK.Grpc.SendQueueMessageResult> SendQueueMessageAsync(QueueMessage queueMessage, Metadata headers, CancellationToken cancellationToken)
        {
            return await TryInvoke<KubeMQ.Contract.SDK.Grpc.SendQueueMessageResult>(async () =>
            {
                return await client.SendQueueMessageAsync(queueMessage, headers: headers, cancellationToken: cancellationToken);
            });
        }

        internal async Task<KubeMQ.Contract.SDK.Grpc.QueueMessagesBatchResponse> SendQueueMessagesBatchAsync(QueueMessagesBatchRequest queueMessagesBatchRequest, Metadata headers, CancellationToken cancellationToken)
        {
            return await TryInvoke<KubeMQ.Contract.SDK.Grpc.QueueMessagesBatchResponse>(async () =>
            {
                return await client.SendQueueMessagesBatchAsync(queueMessagesBatchRequest, headers: headers, cancellationToken: cancellationToken);
            });
        }

        internal KubeMQ.Contract.SDK.Grpc.ReceiveQueueMessagesResponse ReceiveQueueMessages(ReceiveQueueMessagesRequest receiveQueueMessagesRequest, Metadata headers, CancellationToken token)
        {
            var res = TryInvoke<KubeMQ.Contract.SDK.Grpc.ReceiveQueueMessagesResponse>(() =>
            {
                return Task.FromResult(client.ReceiveQueueMessages(receiveQueueMessagesRequest, headers: headers, cancellationToken: token));
            });
            res.Wait(CancellationToken.None);
            if (res.Exception!=null)
                throw res.Exception;
            return res.Result;
        }

        internal async Task<KubeMQ.Contract.SDK.Grpc.Result> SendEventAsync(Event @event, Metadata headers, CancellationToken cancellationToken)
        {
            return await TryInvoke<KubeMQ.Contract.SDK.Grpc.Result>(async () =>
            {
                return await client.SendEventAsync(@event, headers: headers, cancellationToken: cancellationToken);
            });
        }

        internal async Task<KubeMQ.Contract.SDK.Grpc.Response> SendRequestAsync(Request request, Metadata headers, CancellationToken cancellationToken)
        {
            return await TryInvoke<KubeMQ.Contract.SDK.Grpc.Response>(async () =>
            {
                return await client.SendRequestAsync(request, headers: headers, cancellationToken: cancellationToken);
            });
        }

        internal AsyncServerStreamingCall<KubeMQ.Contract.SDK.Grpc.Request> SubscribeToRequests(Subscribe subscribe, Metadata headers, CancellationToken cancellationToken)
        {
            var res = TryInvoke<AsyncServerStreamingCall<KubeMQ.Contract.SDK.Grpc.Request>>(() =>
            {
                return Task.FromResult(client.SubscribeToRequests(subscribe, headers: headers, cancellationToken: cancellationToken));
            });
            res.Wait(CancellationToken.None);
            if (res.Exception!=null)
                throw res.Exception;
            return res.Result;
        }

        internal void SendResponse(Response response, Metadata headers, CancellationToken cancellationToken)
        {
            var res = TryInvoke<int>(() =>
            {
                client.SendResponse(response, headers: headers, cancellationToken: cancellationToken);
                return Task.FromResult(0);
            });
            res.Wait(CancellationToken.None);
            if (res.Exception!=null)
                throw res.Exception;
            CheckDisposed();
            
        }

        internal AsyncServerStreamingCall<EventReceive> SubscribeToEvents(Subscribe subscribe, Metadata headers, CancellationToken cancellationToken)
        {
            var res = TryInvoke<AsyncServerStreamingCall<EventReceive>>(() =>
            {
                return Task.FromResult(client.SubscribeToEvents(subscribe, headers: headers, cancellationToken: cancellationToken));
            });
            res.Wait(CancellationToken.None);
            if (res.Exception!=null)
                throw res.Exception;
            return res.Result;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                    dataLock.WaitOne();
                    channel.Dispose();
                    dataLock.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue=true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~KubeClient()
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
