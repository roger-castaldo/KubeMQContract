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
        private readonly Func<T, R> processMessage;
        private readonly Action<string> errorRecieved;
        private readonly CancellationTokenSource cancellationToken;
        private readonly RPCType commandType;
        private bool active = true;

        public RPCSubscription(KubeSubscription subscription, kubemq.kubemqClient client, ConnectionOptions connectionOptions, Func<T, R> processMessage, Action<string> errorRecieved, CancellationToken cancellationToken, RPCType? commandType, string? responseChannel = null)
        {
            this.subscription = subscription;
            this.client = client;
            this.connectionOptions = connectionOptions;
            this.processMessage = processMessage;
            this.errorRecieved = errorRecieved;
            this.cancellationToken = new CancellationTokenSource();
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
                        while (active && await call.ResponseStream.MoveNext(cancellationToken.Token))
                        {
                            if (active)
                            {
                                var req = call.ResponseStream.Current;
                                var msg = Utility.ConvertMessage<T>(req);
                                if (msg==null)
                                    throw new NullReferenceException(nameof(msg));
                                try
                                {
                                    var result = processMessage(msg);
                                    if (result==null)
                                        throw new NullReferenceException(nameof(result));
                                    var response = new KubeResponse<R>(result, this.connectionOptions, req.ReplyChannel);
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
                        errorRecieved(rpcx.Message);
                    }
                }
                catch (Exception e)
                {
                    errorRecieved(e.Message);
                }

                await Task.Delay(connectionOptions.ReconnectInterval);
            }
        }

        public void Stop()
        {
            active = false;
        }
    }
}
