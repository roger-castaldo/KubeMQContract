namespace KubeMQ.Contract.Interfaces.Messages
{
    internal interface IInternalMessage<T> : IMessage<T>
    {
        Exception? Exception { get; }
    }
}
