using Grpc.Core;
using Grpc.Net.Client;
using Grpc.Net.Client.Configuration;
using KubeMQ.Contract.SDK.Grpc;
using Microsoft.Extensions.Logging;

namespace KubeMQ.Contract.SDK.Connection
{
    internal class KubeClient : IDisposable
    {
        private const int RETRY_COUNT = 5;

        private static readonly ServiceConfig defaultServiceConfig = new()
        {
            MethodConfigs={ 
                new(){
                    Names={ MethodName.Default },
                    RetryPolicy=new()
                    {
                        MaxAttempts=RETRY_COUNT,
                        InitialBackoff = TimeSpan.FromSeconds(1),
                        MaxBackoff = TimeSpan.FromSeconds(5),
                        BackoffMultiplier = 1.5,
                        RetryableStatusCodes = { StatusCode.Unavailable}
                    }
                }
            }
        };

        private readonly string address;
        private readonly ChannelCredentials credentials;
        private readonly ILogger? logger;
        private readonly int messageSize;
        private GrpcChannel channel;
        private kubemq.kubemqClient client;
        private bool disposedValue;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public KubeClient(string address, ChannelCredentials credentials,int messageSize, ILogger? logger)
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        {
            this.address=address;
            this.credentials = credentials;
            this.messageSize=messageSize;
            this.logger = logger;
            ProduceClient();
        }

        private void ProduceClient()
        {
            channel = GrpcChannel.ForAddress(address, new GrpcChannelOptions()
            {
                MaxReceiveMessageSize = messageSize,
                MaxSendMessageSize = messageSize,
                Credentials=credentials,
                HttpHandler = new SocketsHttpHandler
                {
                    KeepAlivePingDelay = TimeSpan.FromSeconds(60),
                    KeepAlivePingTimeout = TimeSpan.FromSeconds(30),
                    KeepAlivePingPolicy = HttpKeepAlivePingPolicy.Always,
                    EnableMultipleHttp2Connections = true
                },
                ServiceConfig=defaultServiceConfig
            });
            client = new(channel);
        }

        private void CheckDisposed()
        {
            if (disposedValue)
                throw new NullReferenceException("Client has already been disposed");
        }

        private R TryInvoke<R>(Func<R> call)
        {
            CheckDisposed();
            Exception? err = null;
            R? result = default;
            try
            {
                result = call();
            }
            catch (RpcException ex)
            {
                err=ex;
                logger?.LogError("KubeClient RPC Error[Message:{},Status:{}]", ex.Message, ex.StatusCode);
            }
            catch (Exception ex)
            {
                err=ex;
                logger?.LogError("KubeClient Error[Message:{}]", ex.Message);
            }
            if (err!=null)
                throw err;
            return result!;
        }

        private async Task<R> TryInvokeAsync<R>(Func<Task<R>> call)
        {
            CheckDisposed();
            Exception? err = null;
            R? result = default;
            try
            {
                result = await call();
            }
            catch (RpcException ex)
            {
                err=ex;
                logger?.LogError("KubeClient RPC Error[Message:{},Status:{}]", ex.Message, ex.StatusCode);
            } catch (Exception ex)
            {
                err=ex;
                logger?.LogError("KubeClient Error[Message:{}]", ex.Message);
            }
            if (err!=null)
                throw err;
            return result!;
        }

        internal KubeMQ.Contract.SDK.Grpc.PingResult Ping()
            => TryInvoke<KubeMQ.Contract.SDK.Grpc.PingResult>(() =>
            {
                return client.Ping(new Empty());
            });

        internal Task<KubeMQ.Contract.SDK.Grpc.SendQueueMessageResult> SendQueueMessageAsync(QueueMessage queueMessage, Metadata headers, CancellationToken cancellationToken)
            => TryInvokeAsync<KubeMQ.Contract.SDK.Grpc.SendQueueMessageResult>(async () =>
            {
                return await client.SendQueueMessageAsync(queueMessage, headers: headers, cancellationToken: cancellationToken);
            });

        internal Task<KubeMQ.Contract.SDK.Grpc.QueueMessagesBatchResponse> SendQueueMessagesBatchAsync(QueueMessagesBatchRequest queueMessagesBatchRequest, Metadata headers, CancellationToken cancellationToken)
            => TryInvokeAsync<KubeMQ.Contract.SDK.Grpc.QueueMessagesBatchResponse>(async () =>
            {
                return await client.SendQueueMessagesBatchAsync(queueMessagesBatchRequest, headers: headers, cancellationToken: cancellationToken);
            });

        internal KubeMQ.Contract.SDK.Grpc.ReceiveQueueMessagesResponse ReceiveQueueMessages(ReceiveQueueMessagesRequest receiveQueueMessagesRequest, Metadata headers, CancellationToken token)
            => TryInvoke<KubeMQ.Contract.SDK.Grpc.ReceiveQueueMessagesResponse>(() =>
            {
                return client.ReceiveQueueMessages(receiveQueueMessagesRequest, headers: headers, cancellationToken: token);
            });

        internal Task<KubeMQ.Contract.SDK.Grpc.Result> SendEventAsync(Event @event, Metadata headers, CancellationToken cancellationToken)
            =>  TryInvokeAsync<KubeMQ.Contract.SDK.Grpc.Result>(async () =>
            {
                return await client.SendEventAsync(@event, headers: headers, cancellationToken: cancellationToken);
            });

        internal Task<KubeMQ.Contract.SDK.Grpc.Response> SendRequestAsync(Request request, Metadata headers, CancellationToken cancellationToken)
            => TryInvokeAsync<KubeMQ.Contract.SDK.Grpc.Response>(async () =>
            {
                return await client.SendRequestAsync(request, headers: headers, cancellationToken: cancellationToken);
            });

        internal AsyncServerStreamingCall<KubeMQ.Contract.SDK.Grpc.Request> SubscribeToRequests(Subscribe subscribe, Metadata headers, CancellationToken cancellationToken)
            => TryInvoke<AsyncServerStreamingCall<KubeMQ.Contract.SDK.Grpc.Request>>(() =>
            {
                return client.SubscribeToRequests(subscribe, headers: headers, cancellationToken: cancellationToken);
            });

        internal Task SendResponseAsync(Response response, Metadata headers, CancellationToken cancellationToken)
            => TryInvokeAsync<Empty>(async () =>
            {
                return await client.SendResponseAsync(response, headers: headers, cancellationToken: cancellationToken);
            });         

        internal AsyncServerStreamingCall<EventReceive> SubscribeToEvents(Subscribe subscribe, Metadata headers, CancellationToken cancellationToken)
            => TryInvoke<AsyncServerStreamingCall<EventReceive>>(() =>
            {
                return client.SubscribeToEvents(subscribe, headers: headers, cancellationToken: cancellationToken);
            });

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                    try
                    {
                        channel.ShutdownAsync().Wait();
                    }catch(Exception ex)
                    {
                        logger?.LogError(ex.Message);
                    }
                    channel.Dispose();
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
