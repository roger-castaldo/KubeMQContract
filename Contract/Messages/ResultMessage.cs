using KubeMQ.Contract.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KubeMQ.Contract.Messages
{
    internal class ResultMessage<T> : TransmittedMessage, IResultMessage<T>
    {
        public T? Response { get; init; }
    }
}
