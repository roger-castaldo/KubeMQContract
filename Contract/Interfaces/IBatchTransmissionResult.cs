using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KubeMQ.Contract.Interfaces
{
    /// <summary>
    /// The return results for a batch queue request.
    /// </summary>
    public interface IBatchTransmissionResult : ITransmissionResult
    {
        /// <summary>
        /// The individual list of Results for each message queued in the batch request.
        /// </summary>
        IEnumerable<ITransmissionResult> Results { get; }
    }
}
