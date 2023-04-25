using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static KubeMQ.Contract.SDK.Grpc.Request.Types;

namespace KubeMQ.Contract.SDK.Interfaces
{
    internal interface IKubeRequest : IKubeMessage
    {
        int Timeout { get; }
        RequestType CommandType { get; }
    }
}
