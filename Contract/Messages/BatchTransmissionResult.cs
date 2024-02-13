using KubeMQ.Contract.Interfaces;

namespace KubeMQ.Contract.Messages
{
    internal class BatchTransmissionResult : TransmissionResult,IBatchTransmissionResult
    {
        public IEnumerable<ITransmissionResult> Results { get; private init; }

        public BatchTransmissionResult(Guid? id= null, string? error = null, IEnumerable<ITransmissionResult>? results= null)
            : base(id, error)
        {
            Results=results??Array.Empty<ITransmissionResult>();
        }
    }
}
