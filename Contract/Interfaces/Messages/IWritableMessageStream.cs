using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KubeMQ.Contract.Interfaces.Messages
{
    /// <summary>
    /// A Writable message Stream designed to transmit the messages of type T
    /// </summary>
    /// <typeparam name="T">The type of message that this stream will be transmitting</typeparam>
    public interface IWritableMessageStream<T> : IMessageStream,IDisposable
    {
        /// <summary>
        /// Write a message of type T to the stream
        /// </summary>
        /// <param name="message">The message to be transmitted</param>
        /// <param name="tagCollection">A collection of headers to transmit along if desired</param>
        /// <param name="cancellationToken">A cancellation token used to cancel the step if necessary</param>
        /// <returns>The result of the message transmission, the write is performed asynchronously and can be ignored if desired.</returns>
        Task<ITransmissionResult> Write(T message, Dictionary<string, string>? tagCollection = null, CancellationToken cancellationToken = default);
    }
}
