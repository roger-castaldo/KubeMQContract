using KubeMQ.Contract.Interfaces.Messages;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Encoding
{
    public class HelloProtoEncoder : IMessageEncoder<HelloProto>
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
