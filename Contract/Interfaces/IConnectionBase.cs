using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KubeMQ.Contract.Interfaces
{
    /// <summary>
    /// Houses the basis of a connection to a KubeMQ host
    /// </summary>
    public interface IConnectionBase
    {
        /// <summary>
        /// Called to Ping the host and get status information
        /// </summary>
        /// <returns>An IPingResult that houses the status and information for the host this connection is linked to</returns>
        IPingResult Ping();
        /// <summary>
        /// Called to remove a subscription from the connection
        /// </summary>
        /// <param name="id">The unique id that was provided by the subscribe call</param>
        void Unsubscribe(Guid id);
    }
}
