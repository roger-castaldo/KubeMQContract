using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KubeMQ.Contract.SDK.Messages
{
    internal class KubeResponse<T> : KubeMessage<T>
    {
        public KubeResponse(T message, ConnectionOptions connectionOptions, string responseChannel) : 
            base(message, connectionOptions, responseChannel)
        {
        }
    }
}
