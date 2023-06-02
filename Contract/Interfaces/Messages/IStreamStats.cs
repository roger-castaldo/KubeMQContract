using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KubeMQ.Contract.Interfaces.Messages
{
    /// <summary>
    /// Houses the current Stream Statistics (Successes recieved/sent, Errors occured)
    /// </summary>
    public interface IStreamStats
    {
        /// <summary>
        /// The total number of Errors that have occured from messages being recieved/sent
        /// </summary>
        ulong Errors { get; }
        /// <summary>
        /// The total number of Messages successfully recieved into or sent from the stream
        /// </summary>
        ulong Success { get; }
    }
}
