using KubeMQ.Contract.Interfaces.Conversion;
using Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Encoding
{
    public class HelloProtoTo1 : IMessageConverter<HelloProto, Hello>
    {
        public Hello Convert(HelloProto source)
        {
            return new Hello()
            {
                FirstName = source.FirstName,
                LastName = source.LastName
            };
        }
    }
}
