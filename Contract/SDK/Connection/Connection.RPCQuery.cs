﻿using Google.Protobuf;
using Grpc.Core;
using KubeMQ.Contract.Attributes;
using KubeMQ.Contract.Interfaces.Connections;
using KubeMQ.Contract.Messages;
using KubeMQ.Contract.SDK.Grpc;
using KubeMQ.Contract.Subscriptions;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace KubeMQ.Contract.SDK.Connection
{
    internal partial class Connection : IRPCQueryConnection
    {
        public async Task<Contract.Interfaces.Messages.IResultMessage<R>> SendRPCQuery<T, R>(T message, string? channel = null, Dictionary<string, string>? tagCollection = null, int? timeout = null, CancellationToken cancellationToken = new CancellationToken())
        {
            try
            {
                var responseFactory = GetMessageFactory<R>();
                var forcedRType = typeof(T).GetCustomAttribute<RPCQueryResponseType>()?.ResponseType;
                if (forcedRType!=null && forcedRType!=typeof(R) && !responseFactory.CanConvertFrom(forcedRType))
                    throw new InvalidQueryResponseTypeSpecified(typeof(T), forcedRType);
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
                return responseFactory.ConvertMessage(logger, res);
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

        public Guid SubscribeRPCQuery<T, R>(Func<Contract.Interfaces.Messages.IMessage<T>, TaggedResponse<R>> processMessage, Action<Exception> errorRecieved, string? channel = null, string group = "", bool ignoreMessageHeader = false, CancellationToken cancellationToken = default)
            => ProduceRPCQuerySubscription<T, R>(processMessage, errorRecieved, channel, group, true,ignoreMessageHeader, cancellationToken);

        public Guid SubscribeRPCQueryAsync<T, R>(Func<Contract.Interfaces.Messages.IMessage<T>, TaggedResponse<R>> processMessage, Action<Exception> errorRecieved, string? channel = null, string group = "", bool ignoreMessageHeader = false, CancellationToken cancellationToken = default)
            => ProduceRPCQuerySubscription<T, R>(processMessage, errorRecieved, channel, group, false,ignoreMessageHeader, cancellationToken);

        private Guid ProduceRPCQuerySubscription<T, R>(
            Func<Contract.Interfaces.Messages.IMessage<T>, TaggedResponse<R>> processMessage,
            Action<Exception> errorRecieved,
            string? channel,
            string group,
            bool synchronous,
            bool ignoreMessageHeader,
            CancellationToken cancellationToken
        )
        {
            var responseFactory = GetMessageFactory<R>();
            var forcedRType = typeof(T).GetCustomAttribute<RPCQueryResponseType>()?.ResponseType;
            if (forcedRType!=null && forcedRType!=typeof(R) && !responseFactory.CanConvertFrom(forcedRType))
                throw new InvalidQueryResponseTypeSpecified(typeof(T),forcedRType);
            var id = Guid.NewGuid();
            var sub = RegisterSubscription<RPCQuerySubscription<T, R>>(
                new RPCQuerySubscription<T, R>(
                    id,
                    GetMessageFactory<T>(ignoreMessageHeader), 
                    responseFactory, 
                    new KubeSubscription<T>(clientID, id, channel: channel, group: group),
                    EstablishConnection(),
                    this.connectionOptions, 
                    new Func<Contract.Interfaces.Messages.IMessage<T>, Task<TaggedResponse<R>>>(msg =>
                    {
                        try
                        {
                            return Task.FromResult(processMessage(msg));
                        }catch(Exception){ throw; }
                    }), 
                    errorRecieved, 
                    ProduceLogger(id),
                    synchronous,
                    cancellationToken: cancellationToken
                )
            );
            Log(LogLevel.Information, "Registered SubscribeRPCQuery {} of type {}", sub.ID, Utility.TypeName<T>());
            return sub.ID;
        }
    }
}
