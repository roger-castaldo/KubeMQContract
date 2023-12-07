namespace KubeMQ.Contract.Interfaces.Messages
{
    internal struct SRecievedMessage<T>
    {
        public DateTime Timestamp { get; private init; }
        public T Data { get; private init; }

        public SRecievedMessage(T data)
        {
            Timestamp=DateTime.Now;
            Data=data;
        }
    }
}
