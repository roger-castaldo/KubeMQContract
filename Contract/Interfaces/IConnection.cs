using Google.Protobuf;
using KubeMQ.Contract.SDK.Grpc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KubeMQ.Contract.Interfaces
{
    /// <summary>
    /// The main component of this library.  Houses a connection to a KubeMQ host and is used to Send and Recieve messages
    /// </summary>
    public interface IConnection : IPubSubConnection,IQueueConnection,IRPCConnection
    {
    }
}
