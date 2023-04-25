using KubeMQ.Contract.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KubeMQ.Contract.Messages
{
    internal class BatchTransmissionResult : TransmissionResult,IBatchTransmissionResult
    {
        public IEnumerable<ITransmissionResult> Results { get; init; } = Array.Empty<ITransmissionResult>();
    }
}
