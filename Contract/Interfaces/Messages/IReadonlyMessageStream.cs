namespace KubeMQ.Contract.Interfaces.Messages
{
    /// <summary>
    /// A readonly message stream implemented as an Asynchronous Enumerable for the messages coming in from a subscription
    /// </summary>
    /// <typeparam name="T">The type of message to be listened for as a subscription</typeparam>
    public interface IReadonlyMessageStream<T> : IMessageStream,IAsyncEnumerable<IMessage<T>> 
    {
    }
}
