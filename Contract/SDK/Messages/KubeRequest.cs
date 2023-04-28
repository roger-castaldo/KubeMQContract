using KubeMQ.Contract.Attributes;
using KubeMQ.Contract.SDK.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static KubeMQ.Contract.SDK.Grpc.Request.Types;

namespace KubeMQ.Contract.SDK.Messages
{
    internal class KubeRequest<T,R> : KubeMessage<T>,IKubeRequest
    {
        private readonly int timeout;
        public int Timeout => timeout;
        private readonly RPCType commandType;
        public RequestType CommandType => (RequestType)(int)commandType;
        public KubeRequest(T message, ConnectionOptions connectionOptions, int? timeout, string? channel, RPCType? type, Dictionary<string, string>? tagCollection)
            : base(message, connectionOptions, channel, tagCollection) {
            type = type??(typeof(T).GetCustomAttributes<RPCCommandType>().Any() ? typeof(T).GetCustomAttributes<RPCCommandType>().First().Type : null);
            if (type==null)
                throw new ArgumentNullException(nameof(type), "message must have an RPC type value");
            commandType=type.Value;
            this.timeout = timeout??typeof(T).GetCustomAttributes<MessageResponseTimeout>().Select(mrt => mrt.Value).FirstOrDefault(5000);
        }
    }
}
