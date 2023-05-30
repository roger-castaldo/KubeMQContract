using KubeMQ.Contract.Interfaces;
using KubeMQ.Contract.Subscriptions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KubeMQ.Contract.SDK
{
    internal partial class Connection : IPubSubStreamConnection
    {
        public IReadonlyMessageStream<T> SubscribeToStream<T>(Action<Exception> errorRecieved, CancellationToken cancellationToken = default, string? channel = null, string group = "", long storageOffset = 0, MessageReadStyle? messageReadStyle = null)
        {
            var stream = new ReadonlyMessageStream<T>(GetMessageFactory<T>(), new KubeSubscription<T>(this.connectionOptions, channel: channel, group: group), this.client, this.connectionOptions, errorRecieved, storageOffset, this, messageReadStyle, cancellationToken);
            Log(LogLevel.Information, "Requesting MessageStream {} of type {}", stream.ID, typeof(T).Name);
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            stream.Start();
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            lock (subscriptions)
            {
                subscriptions.Add(stream);
            }
            return stream;
        }

        public IWritableMessageStream<T> CreateStream<T>(string? channel = null)
        {
            var stream = new WritableMessageStream<T>(this, channel);
            Log(LogLevel.Information, "Producing a WritableStream of type {}", typeof(T).Name);
            return stream;
        }
    }
}
