using KubeMQ.Contract.Interfaces;
using KubeMQ.Contract.Interfaces.Connections;
using KubeMQ.Contract.Interfaces.Messages;

namespace KubeMQ.Contract.Subscriptions
{
    internal class WritableMessageStream<T> : MessageStream,IWritableMessageStream<T>
    {
        private readonly IPubSubConnection connection;
        private readonly string? channel;
        

        public WritableMessageStream(IPubSubConnection connection, string? channel)
        {
            this.connection=connection;
            this.channel=channel;
        }

        

        public async Task<ITransmissionResult> Write(T message, Dictionary<string, string>? tagCollection = null, CancellationToken cancellationToken = default)
        {
            var result = await this.connection.Send<T>(message, cancellationToken:cancellationToken, channel:this.channel, tagCollection: tagCollection);
            if (result.IsError)
                errors++;
            else
                success++;
            return result;
        }
    }
}
