using KubeMQ.Contract.Interfaces.Messages;
using ProtoBuf;

namespace Encoding
{
    public class HelloProtoEncoder : IMessageTypeEncoder<HelloProto>
    {
        public HelloProto? Decode(Stream stream)
        {
            return Serializer.Deserialize<HelloProto>(stream);
        }

        public byte[] Encode(HelloProto message)
        {
            var result = Array.Empty<byte>();
            using (var ms = new MemoryStream())
            {
                Serializer.Serialize<HelloProto>(ms, message);
                result = ms.ToArray();
            }
            return result;
        }
    }
}
