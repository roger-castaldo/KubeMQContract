using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KubeMQ.Contract.Interfaces
{
    /// <summary>
    /// Represents a Message Queue on the KubeMQ server of type T
    /// </summary>
    /// <typeparam name="T">The type of messages that should reside in that queue</typeparam>
    public interface IMessageQueue<T>
    {
        /// <summary>
        /// A unique ID representing the queue
        /// </summary>
        Guid ID { get; }
        /// <summary>
        /// Returns true if there are any more messages in the queue
        /// </summary>
        bool HasMore { get; }
        /// <summary>
        /// Called to peek a message from the queue (this will not remove the message from the queue)
        /// </summary>
        /// <returns>null if there are no messages available on the queue, otherwise, returns the first available message in the queue
        /// without removing it from the queue</returns>
        IMessage<T>? Peek();
        /// <summary>
        /// Called to pop a message from the queue (this WILL remove the message from the queue)
        /// </summary>
        /// <returns>null if there are no message available on the queue, otherwise, returns the first available message in the queue</returns>
        IMessage<T>? Pop();
        /// <summary>
        /// Called to pop at least {count} messages from the queue (this WILL remove the messages returned from the queue)
        /// </summary>
        /// <param name="count">The number of messages to pop from the queue</param>
        /// <returns>an enumeration of messages from the queue of the max length {count} or an emtpy result if none available</returns>
        IEnumerable<IMessage<T>> Pop(int count);
    }
}
