using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KubeMQ.Contract.Interfaces.Messages
{
    /// <summary>
    /// Represents a message recieved from the system (be it PubSub, RPC, Queue) 
    /// </summary>
    /// <typeparam name="T">The type of message recieved</typeparam>
    public interface IMessage<T> : IMessageHeader
    {
        /// <summary>
        /// The ID of the message that was generated during transmission
        /// </summary>
        string ID { get; }
        /// <summary>
        /// The timestamp of when the message was recieved
        /// </summary>
        DateTime Timestamp { get; }
        /// <summary>
        /// Houses the Message itself
        /// </summary>
        T Data { get; }
    }
}
