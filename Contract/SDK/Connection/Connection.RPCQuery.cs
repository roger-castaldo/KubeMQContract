using Google.Protobuf;
using Grpc.Core;
using KubeMQ.Contract.Interfaces.Connections;
using KubeMQ.Contract.Messages;
using KubeMQ.Contract.SDK.Grpc;
using KubeMQ.Contract.Subscriptions;
using Microsoft.Extensions.Logging;

namespace KubeMQ.Contract.SDK.Connection
{
    internal partial class Connection : IRPCQueryConnection
    {
        public async Task<Contract.Interfaces.Messages.IResultMessage<R>> SendRPCQuery<T, R>(T message, string? channel = null, Dictionary<string, string>? tagCollection = null, int? timeout = null, CancellationToken cancellationToken = new CancellationToken())
        {
            try
            {
                var msg = GetMessageFactory<T>().Request(message, connectionOptions,clientID, channel, tagCollection, timeout, Request.Types.RequestType.Query);
                Log(LogLevel.Information, "Sending RPC Message {} of type {}", msg.ID, Utility.TypeName<T>());
                var res = await client.SendRequestAsync(new Request()
                {
                    RequestID=msg.ID,
                    RequestTypeData = msg.CommandType,
                    Timeout = msg.Timeout,
                    ClientID = msg.ClientID,
                    Channel = msg.Channel,
                    Metadata = msg.MetaData,
                    Body = ByteString.CopyFrom(msg.Body),
                    Tags = { msg.Tags }
                }, connectionOptions.GrpcMetadata, cancellationToken);
                if (res==null)
                {
                    Log(LogLevel.Error, "Transmission Result for RPC {} is null", msg.ID);
                    return new ResultMessage<R>()
                    {
                        IsError=true,
                        Error="null response recieved from KubeMQ server"
                    };
                }
                Log(LogLevel.Information, "Transmission Result for RPC {} (IsError:{},Error:{})", msg.ID, !string.IsNullOrEmpty(res.Error), res.Error);
                if (!res.Executed || !string.IsNullOrEmpty(res.Error))
                    return new ResultMessage<R>()
                    {
                        IsError=true,
                        Error=res.Error
                    };
                return GetMessageFactory<R>().ConvertMessage(logger, res);
            }
            catch (RpcException ex)
            {
                Log(LogLevel.Error, "RPC error occured on SendRPC in send Message:{}, Status: {}", ex.Message, ex.Status);
                return new ResultMessage<R>()
                {
                    IsError=true,
                    Error=$"Message: {ex.Message}, Status: {ex.Status}"
                };
            }
            catch (Exception ex)
            {
                Log(LogLevel.Error, "Exception occured in SendRPC Message:{}, Status: {}", ex.Message);
                return new ResultMessage<R>()
                {
                    IsError=true,
                    Error=ex.Message
                };
            }
        }

        public Guid SubscribeRPCQuery<T, R>(
            Func<Contract.Interfaces.Messages.IMessage<T>, Task<TaggedResponse<R>>> processMessage,
            Action<Exception> errorRecieved,
            string? channel = null,
            string group = "",
            CancellationToken cancellationToken = new CancellationToken()
        )
        {
            var id = Guid.NewGuid();
            var sub = RegisterSubscription<RPCQuerySubscription<T, R>>(
                new RPCQuerySubscription<T, R>(
                    id,
                    GetMessageFactory<T>(), 
                    GetMessageFactory<R>(), 
                    new KubeSubscription<T>(clientID, id, channel: channel, group: group),
                    EstablishConnection(),
                    this.connectionOptions, 
                    processMessage, 
                    errorRecieved, 
                    ProduceLogger(id),
                    cancellationToken: cancellationToken
                )
            );
            Log(LogLevel.Information, "Registered SubscribeRPCQuery {} of type {}", sub.ID, Utility.TypeName<T>());
            return sub.ID;
        }
    }
}
