using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using KubeMQ.Contract.Attributes;
using KubeMQ.Contract.Interfaces;
using KubeMQ.Contract.SDK;
using KubeMQ.Contract.SDK.Grpc;
using KubeMQ.Contract.SDK.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace KubeMQ.Contract.Subscriptions
{
    internal class RPCSubscription<T,R> : IMessageSubscription
    {
        public Guid ID => Guid.NewGuid();
        private readonly KubeSubscription subscription;
        private readonly kubemq.kubemqClient client;
        private readonly ConnectionOptions connectionOptions;
        private readonly Func<IMessage<T>, TaggedResponse<R>> processMessage;
        private readonly Action<string> errorRecieved;
        private readonly CancellationTokenSource cancellationToken;
        private readonly RPCType commandType;
        private readonly ILogProvider logProvider;
        private bool active = true;

        public RPCSubscription(KubeSubscription subscription, kubemq.kubemqClient client, ConnectionOptions connectionOptions, Func<IMessage<T>, TaggedResponse<R>> processMessage, Action<string> errorRecieved, CancellationToken cancellationToken,ILogProvider logProvider, RPCType? commandType, string? responseChannel = null)
        {
            this.subscription = subscription;
            this.client = client;
            this.connectionOptions = connectionOptions;
            this.processMessage = processMessage;
            this.errorRecieved = errorRecieved;
            this.cancellationToken = new CancellationTokenSource();
            this.logProvider=logProvider;
            commandType = commandType??(typeof(T).GetCustomAttributes<RPCCommandType>().Any() ? typeof(T).GetCustomAttributes<RPCCommandType>().First().Type : null);
            if (commandType==null)
                throw new ArgumentNullException(nameof(commandType), "message must have an RPC type value");
            
            this.commandType = commandType.Value;

            cancellationToken.Register(() =>
            {
                active = false;
                this.cancellationToken.Cancel();
            });

            start();
        }

        private async Task start()
        {
            logProvider.LogTrace("Attempting to establish RPC subscription {} to {} on channel {} for type {} returning type {}", ID, connectionOptions.Address, subscription.Channel, typeof(T).Name,typeof(R).Name);
            while (active && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    using (var call = client.SubscribeToRequests(new Subscribe()
                    {
                        Channel = subscription.Channel,
                        ClientID = subscription.ClientID,
                        Group = subscription.Group,
                        SubscribeTypeData = (Subscribe.Types.SubscribeType)((int)this.commandType+2)
                    },
                        connectionOptions.GrpcMetadata,
                        null, cancellationToken.Token))
                    {
                        logProvider.LogTrace("Connection for RPC subscription {} established", ID);
                        while (active && await call.ResponseStream.MoveNext(cancellationToken.Token))
                        {
                            if (active)
                            {
                                var req = call.ResponseStream.Current;
                                logProvider.LogTrace("Message recieved {} on RPC subscription {}", req.RequestID, ID);
                                var msg = Utility.ConvertMessage<T>(logProvider,req);
                                try
                                {
                                    if (msg==null)
                                        throw new NullReferenceException(nameof(msg));
                                    var result = processMessage(msg);
                                    if (result==null)
                                        throw new NullReferenceException(nameof(result));
                                    var response = new KubeResponse<R>(result.Response, this.connectionOptions, req.ReplyChannel,result.Tags);
                                    logProvider.LogTrace("Response generated for {} on RPC subscription {}", req.RequestID, ID);
                                    client.SendResponse(new Response()
                                    { 
                                        CacheHit=false,
                                        RequestID= req.RequestID,
                                        ClientID=subscription.ClientID,
                                        Executed=true,
                                        Error=string.Empty,
                                        ReplyChannel=response.Channel,
                                        Body=Google.Protobuf.ByteString.CopyFrom(response.Body),
                                        Metadata=response.MetaData,
                                        Tags = { response.Tags },
                                        Timestamp=Utility.ToUnixTime(DateTime.Now)
                                    }, headers:connectionOptions.GrpcMetadata, deadline:null, cancellationToken:cancellationToken.Token);
                                }catch(Exception e)
                                {
                                    logProvider.LogError("Message {} failed on subscription {}.  Message:{}", req.RequestID, ID, e.Message);
                                    client.SendResponse(new Response()
                                    {
                                        RequestID=req.RequestID,
                                        ClientID=subscription.ClientID,
                                        Executed=true,
                                        Error=e.Message,
                                        ReplyChannel=req.ReplyChannel,
                                        Body=Google.Protobuf.ByteString.Empty,
                                        Timestamp=Utility.ToUnixTime(DateTime.Now)
                                    }, headers: connectionOptions.GrpcMetadata, deadline: null, cancellationToken: cancellationToken.Token);
                                }
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
                        logProvider.LogError("RPC Error recieved on RPC subscription {}.  StatusCode:{},Message:{}", ID, rpcx.StatusCode, rpcx.Message);
                        errorRecieved(rpcx.Message);
                    }
                }
                catch (Exception e)
                {
                    logProvider.LogError("Error recieved on RPC subscription {}.  Message:{}", ID, e.Message);
                    errorRecieved(e.Message);
                }

                await Task.Delay(connectionOptions.ReconnectInterval);
            }
        }

        public void Stop()
        {
            logProvider.LogTrace("Stop called for RPC subscription {}", ID);
            active = false;
        }
    }
}
