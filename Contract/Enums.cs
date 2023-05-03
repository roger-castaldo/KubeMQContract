using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KubeMQ.Contract
{
    /// <summary>
    /// These are the different read styles to use when subscribing to a stored Event PubSub
    /// </summary>
    public enum MessageReadStyle
    {
        /// <summary>
        /// Start from the new ones (unread ones) only
        /// </summary>
        StartNewOnly = 1,
        /// <summary>
        /// Start at the beginning
        /// </summary>
        StartFromFirst = 2,
        /// <summary>
        /// Start at the last message
        /// </summary>
        StartFromLast = 3,
        /// <summary>
        /// Start at message number X (this value is specified when creating the listener)
        /// </summary>
        StartAtSequence = 4,
        /// <summary>
        /// Start at time X (this value is specified when creating the listener)
        /// </summary>
        StartAtTime = 5,
        /// <summary>
        /// Start at Time Delte X (this value is specified when creating the listener)
        /// </summary>
        StartAtTimeDelta = 6
    };

    /// <summary>
    /// Specifies the RPC style to use.  This style must match on both the transmitter and the 
    /// responder.
    /// </summary>
    public enum RPCType
    {
        /// <summary>
        /// Use the Command Style
        /// </summary>
        Command=1, 
        /// <summary>
        /// Use a Query Style
        /// </summary>
        Query=2
    }
}
