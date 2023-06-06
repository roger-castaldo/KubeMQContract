using KubeMQ.Contract.Interfaces.Messages;
using System.Text;

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
