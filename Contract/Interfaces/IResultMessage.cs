using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KubeMQ.Contract.Interfaces
{
    /// <summary>
    /// Houses a Response from an RPC call
    /// </summary>
    /// <typeparam name="T">The type of response expected</typeparam>
    public interface IResultMessage<T> : IMessageHeader, ITransmissionResult
    {
        /// <summary>
        /// Houses the Response, if one is returned, from the RPC call
        /// </summary>
        T? Response { get; }
    }
}
