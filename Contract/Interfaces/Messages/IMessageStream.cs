using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KubeMQ.Contract.Interfaces.Messages
{
    /// <summary>
    /// Houses the common attributes that exist within both a ReadOnly and Writeable stream
    /// </summary>
    public interface IMessageStream
    {
        /// <summary>
        /// The total number of messages sent/recieved
        /// </summary>
        ulong Length { get; }
        /// <summary>
        /// The current statistics for the stream
        /// </summary>
        IStreamStats Stats { get; }
    }
}
