namespace KubeMQ.Contract.Interfaces.Messages
{
    /// <summary>
    /// Represents a message recieved from the system (be it PubSub, RPC, Queue) 
    /// </summary>
    /// <typeparam name="T">The type of message recieved</typeparam>
    public interface IMessage<T> : IDisposable
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
        /// The timestamp of when the message was converted
        /// </summary>
        DateTime ConversionTimestamp { get; }
        /// <summary>
        /// The headers supplied from the KubeMQ message
        /// </summary>
        IMessageHeader Headers { get; }
        /// <summary>
        /// Houses the Message itself
        /// </summary>
        T? Data { get; }
    }
}
