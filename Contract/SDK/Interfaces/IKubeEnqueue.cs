using KubeMQ.Contract.SDK.Grpc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KubeMQ.Contract.SDK.Interfaces
{
    internal interface IKubeEnqueue
    {
        int? DelaySeconds { get; }
        int? ExpirationSeconds { get; }
        int? MaxSize { get; }
        string? MaxCountChannel { get; }
        QueueMessagePolicy Policy { get; }
        QueueMessageAttributes Attributes { get; }
    }
}
