using KubeMQ.Contract.Interfaces.Messages;
using System.Text.Json;

namespace KubeMQ.Contract.Factories
{
    internal class JsonEncoder<T> : IMessageTypeEncoder<T> 
    {
        private static readonly JsonSerializerOptions options = new()
        {
            WriteIndented=false,
            DefaultBufferSize=4096,
            AllowTrailingCommas=true,
            PropertyNameCaseInsensitive=true,
            ReadCommentHandling=JsonCommentHandling.Skip
        };

        public T? Decode(Stream stream) => JsonSerializer.Deserialize<T>(stream,options:options);

        public byte[] Encode(T message) => System.Text.UTF8Encoding.UTF8.GetBytes(JsonSerializer.Serialize<T>(message, options: options));
    }
}
