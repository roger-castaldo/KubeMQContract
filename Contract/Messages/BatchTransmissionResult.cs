using KubeMQ.Contract.Interfaces;

namespace KubeMQ.Contract.Messages
{
    internal class BatchTransmissionResult : TransmissionResult,IBatchTransmissionResult
    {
        public IEnumerable<ITransmissionResult> Results { get; init; } = Array.Empty<ITransmissionResult>();
    }
}
