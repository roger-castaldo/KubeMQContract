﻿using Google.Protobuf;
using KubeMQ.Contract.SDK.Grpc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KubeMQ.Contract.Interfaces.Connections
{
    /// <summary>
    /// The main component of this library.  Houses a connection to a KubeMQ host and is used to Send and Recieve messages
    /// </summary>
    public interface IConnection : IPubSubConnection, IPubSubStreamConnection, IQueueConnection,IRPCQueryConnection,IRPCCommandConnection
    {
    }
}
