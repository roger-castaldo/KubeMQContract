﻿using Google.Protobuf;
using Grpc.Core;
using KubeMQ.Contract.Interfaces.Connections;
using KubeMQ.Contract.Interfaces.Messages;
using KubeMQ.Contract.Messages;
using KubeMQ.Contract.SDK.Grpc;
using KubeMQ.Contract.Subscriptions;
using Microsoft.Extensions.Logging;

namespace KubeMQ.Contract.SDK.Connection
{
    internal partial class Connection : IRPCCommandConnection
    {
        public async Task<IResultMessage<bool>> SendRPCCommand<T>(T message, string? channel = null, Dictionary<string, string>? tagCollection = null, int? timeout = null, CancellationToken cancellationToken = default)
        {
            try
            {
                var msg = GetMessageFactory<T>().Request(message, connectionOptions,clientID, channel, tagCollection, timeout, Request.Types.RequestType.Command);
                Log(LogLevel.Information, "Sending RPC Command {} of type {}", msg.ID, Utility.TypeName<T>());
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
                    return new ResultMessage<bool>()
                    {
                        IsError=true,
                        Error="null response recieved from KubeMQ server"
                    };
                }
                Log(LogLevel.Information, "Transmission Result for RPC {} (IsError:{},Error:{})", msg.ID, !string.IsNullOrEmpty(res.Error), res.Error);
                if (!res.Executed || !string.IsNullOrEmpty(res.Error))
                    return new ResultMessage<bool>()
                    {
                        IsError=true,
                        Error=res.Error,
                        Tags=res.Tags
                    };
                return new ResultMessage<bool>()
                {
                    Response=res.Executed,
                    Tags=res.Tags
                };
            }
            catch (RpcException ex)
            {
                Log(LogLevel.Error, "RPC error occured on SendRPC in send Message:{}, Status: {}", ex.Message, ex.Status);
                return new ResultMessage<bool>()
                {
                    IsError=true,
                    Error=$"Message: {ex.Message}, Status: {ex.Status}"
                };
            }
            catch (Exception ex)
            {
                Log(LogLevel.Error, "Exception occured in SendRPC Message:{}, Status: {}", ex.Message);
                return new ResultMessage<bool>()
                {
                    IsError=true,
                    Error=ex.Message
                };
            }
        }

        public Guid SubscribeRPCCommand<T>(Func<Contract.Interfaces.Messages.IMessage<T>, TaggedResponse<bool>> processMessage, Action<Exception> errorRecieved, string? channel = null, string group = "", bool ignoreMessageHeader = false, CancellationToken cancellationToken = default)
            => ProduceRPCCommandSubscription<T>(processMessage,errorRecieved,channel,group,true,ignoreMessageHeader,cancellationToken);

        public Guid SubscribeRPCCommandAsync<T>(Func<Contract.Interfaces.Messages.IMessage<T>, TaggedResponse<bool>> processMessage, Action<Exception> errorRecieved, string? channel = null, string group = "", bool ignoreMessageHeader = false, CancellationToken cancellationToken = default)
            => ProduceRPCCommandSubscription<T>(processMessage, errorRecieved, channel, group, false,ignoreMessageHeader, cancellationToken);

        public Guid ProduceRPCCommandSubscription<T>(
            Func<Contract.Interfaces.Messages.IMessage<T>, TaggedResponse<bool>> processMessage, 
            Action<Exception> errorRecieved, 
            string? channel, 
            string group, 
            bool synchronous,
            bool ignoreMessageHeader,
            CancellationToken cancellationToken)
        {
            var id = Guid.NewGuid();
            var sub = RegisterSubscription<RPCCommandSubscription<T>>(
                new RPCCommandSubscription<T>(
                    id,
                    GetMessageFactory<T>(),
                    new KubeSubscription<T>(clientID, id, channel: channel, group: group),
                    EstablishConnection(),
                    this.connectionOptions,
                    new Func<Contract.Interfaces.Messages.IMessage<T>, Task<TaggedResponse<bool>>>(msg =>
                    {
                        try
                        {
                            return Task.FromResult(processMessage(msg));
                        }
                        catch (Exception) { throw; }
                    }),
                    errorRecieved,
                    ProduceLogger(id),
                    synchronous,
                    cancellationToken: cancellationToken
                 )
            );
            Log(LogLevel.Information, "Registered SubscribeRPCCommand {} of type {}", sub.ID, Utility.TypeName<T>());
            return sub.ID;
        }
    }
}
