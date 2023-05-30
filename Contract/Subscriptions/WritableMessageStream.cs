using KubeMQ.Contract.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KubeMQ.Contract.Subscriptions
{
    internal class WritableMessageStream<T> : IWritableMessageStream<T>
    {
        private class StreamStats : IStreamStats
        {
            public long Errors { get; init; }
            public long Success { get; init; }
        }

        private readonly IPubSubConnection connection;
        private readonly string channel;
        private long success=0;
        private long errors=0;

        public WritableMessageStream(IPubSubConnection connection, string channel)
        {
            this.connection=connection;
            this.channel=channel;
        }

        public long Length => success+errors;

        public IStreamStats Stats => new StreamStats()
        {
            Errors=errors,
            Success=success
        };

        public void Dispose()
        {
            errors=0; 
            success=0;    
        }

        public async Task<ITransmissionResult> Write(T message, CancellationToken cancellationToken = default, Dictionary<string, string>? tagCollection = null)
        {
            var result = await this.connection.Send<T>(message, cancellationToken, this.channel, tagCollection);
            if (result.IsError)
                errors++;
            else
                success++;
            return result;
        }
    }
}
