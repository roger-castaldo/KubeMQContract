using KubeMQ.Contract.Interfaces.Conversion;
using Messages;

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
