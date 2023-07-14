using KubeMQ.Contract.Attributes;
using ProtoBuf;

namespace Encoding
{
    [MessageChannel("Greeting.Proto")]
    [ProtoContract]
    public class HelloProto
    {
        [ProtoMember(1)]
        public string FirstName { get; set; } = string.Empty;
        [ProtoMember(2)]
        public string LastName { get; set; } = string.Empty;
    }
}
