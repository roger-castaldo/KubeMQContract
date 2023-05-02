using KubeMQ.Contract.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KubeMQ.Contract.Factories
{
    internal class JsonEncoder<T> : IMessageEncoder<T>
    {
        public T? Decode(Stream stream)
        {
            return System.Text.Json.JsonSerializer.Deserialize<T>(stream);
        }

        public byte[] Encode(T message)
        {
            return Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(message, new System.Text.Json.JsonSerializerOptions()
            {
                WriteIndented = false
            }));
        }
    }
}
