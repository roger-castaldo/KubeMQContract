namespace KubeMQ.Contract.Interfaces.Messages
{
    internal interface IMessageSubscription : IDisposable
    {
        Guid ID { get; }
        void Start();
        void Stop();
    }
}
