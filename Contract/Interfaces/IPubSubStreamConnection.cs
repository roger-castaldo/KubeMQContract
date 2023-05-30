using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KubeMQ.Contract.Interfaces
{
    public interface IPubSubStreamConnection : IConnectionBase
    {
        /// <summary>
        /// Called to Send a Message in a Pub/Sub style
        /// </summary>
        /// <typeparam name="T">The type of messages being sent</typeparam>
        /// <param name="channel">The name of the channel to transmit into.  If this is not specified here, a MessageChannel attribute is expected on T</param>
        /// <returns>A writable transmission stream design to write the connection send methods</returns>
        IWritableMessageStream<T> CreateStream<T>(string? channel = null);
        IReadonlyMessageStream<T> SubscribeToStream<T>(
            Action<Exception> errorRecieved,
            CancellationToken cancellationToken = new CancellationToken(),
            string? channel = null,
            string group = "",
            long storageOffset = 0,
            MessageReadStyle? messageReadStyle = null);

    }
}
