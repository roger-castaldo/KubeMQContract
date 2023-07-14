namespace KubeMQ.Contract.SDK.Interfaces
{
    internal interface IKubeEvent : IKubeMessage
    {
        bool Stored { get; }
    }
}
