using Google.Protobuf;
using Google.Protobuf.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KubeMQ.Contract.Messages
{
    internal class Message<T> : TransmittedMessage, KubeMQ.Contract.Interfaces.IMessage<T>
    {
        public T Data { get; init; }
    }
}
