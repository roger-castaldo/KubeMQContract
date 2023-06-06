using KubeMQ.Contract.Interfaces.Messages;

namespace KubeMQ.Contract.Messages
{
    internal class ResultMessage<T> : TransmittedMessage, IResultMessage<T>
    {
        public T? Response { get; init; }
    }
}
