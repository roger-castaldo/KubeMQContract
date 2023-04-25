using KubeMQ.Contract.SDK.Grpc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KubeMQ.Contract.SDK.Interfaces
{
    internal interface IKubeBatchEnqueue
    {
        Guid ID { get; }
        IEnumerable<QueueMessage> Messages { get; }
    }
}
