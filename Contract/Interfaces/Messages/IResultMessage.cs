using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KubeMQ.Contract.Interfaces.Messages
{
    /// <summary>
    /// Houses a response from an RPC call
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IResultMessage<T> : ITransmissionResult,IMessageHeader
    {
        /// <summary>
        /// Houses the Response, if one is returned, from the RPC call
        /// </summary>
        T? Response { get; }
    }
}
