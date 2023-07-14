using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KubeMQ.Contract
{
    /// <summary>
    /// Thrown when an error occurs attempting to connect to the KubeMQ server.  
    /// Specifically this will be thrown when the Ping that is executed on each initial connection fails.
    /// </summary>
    public class UnableToConnect : Exception
    {
        internal UnableToConnect()
            : base("Unable to establish connection to the KubeMQ host") { }
    }
}
