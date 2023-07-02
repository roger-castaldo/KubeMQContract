﻿using Google.Protobuf;
using Grpc.Core;
using KubeMQ.Contract.Interfaces;
using KubeMQ.Contract.Interfaces.Connections;
using KubeMQ.Contract.Messages;
using KubeMQ.Contract.SDK.Grpc;
using KubeMQ.Contract.Subscriptions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KubeMQ.Contract.SDK.Connection
{
    internal partial class Connection : IRPCQueryConnection
    {
        public async Task<Contract.Interfaces.Messages.IResultMessage<R>> SendRPCQuery<T, R>(T message, string? channel = null, Dictionary<string, string>? tagCollection = null, int? timeout = null, CancellationToken cancellationToken = new CancellationToken())
        {
            try
            {
                var msg = GetMessageFactory<T>().Request(message, connectionOptions, channel, tagCollection, timeout, Request.Types.RequestType.Query);
                Log(LogLevel.Information, "Sending RPC Message {} of type {}", msg.ID, typeof(T).Name);
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
                }, connectionOptions.GrpcMetadata, null, cancellationToken);
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
                return GetMessageFactory<R>().ConvertMessage(this, res);
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
            Func<Contract.Interfaces.Messages.IMessage<T>, TaggedResponse<R>> processMessage,
            Action<Exception> errorRecieved,
            string? channel = null,
            string group = "",
            CancellationToken cancellationToken = new CancellationToken()
        )
        {
            var sub = new RPCQuerySubscription<T, R>(GetMessageFactory<T>(), GetMessageFactory<R>(), new KubeSubscription<T>(this.connectionOptions, channel: channel, group: group),
                this.client,
                this.connectionOptions, processMessage, errorRecieved, this,
                cancellationToken: cancellationToken);
            Log(LogLevel.Information, "Requesting SubscribeRPC {} of type {}", sub.ID, typeof(T).Name);
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            sub.Start();
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            lock (subscriptions)
            {
                subscriptions.Add(sub);
            }
            return sub.ID;
        }
    }
}