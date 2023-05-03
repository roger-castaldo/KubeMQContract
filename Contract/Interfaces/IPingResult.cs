using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KubeMQ.Contract.Interfaces
{
    /// <summary>
    /// Represents a Ping Result from the KubeMQ server
    /// </summary>
    public interface IPingResult
    {
        /// <summary>
        /// The name/IP of the host that was pinged
        /// </summary>
        string Host { get; }
        /// <summary>
        /// The version string of the host that was pinged
        /// </summary>
        string Version { get; }
        /// <summary>
        /// The Server Start Time of the host that was pinged
        /// </summary>
        DateTime ServerStartTime { get; }
        /// <summary>
        /// The Server Up Time of the host that was pinged
        /// </summary>
        TimeSpan ServerUpTime { get; }
    }
}
