using KubeMQ.Contract.Interfaces.Messages;

namespace KubeMQ.Contract.Factories
{
    internal class JsonEncoder<T> : IMessageEncoder<T> 
    {
        private static readonly System.Text.Json.JsonSerializerOptions options = new()
        {
            WriteIndented=false,
            DefaultBufferSize=4096,
            AllowTrailingCommas=true,
            PropertyNameCaseInsensitive=true,
            ReadCommentHandling=System.Text.Json.JsonCommentHandling.Skip
        };

        public T? Decode(Stream stream)
        {
            return System.Text.Json.JsonSerializer.Deserialize<T>(stream,options:options);
        }

        public byte[] Encode(T message)
        {
            using var ms = new MemoryStream();
            System.Text.Json.JsonSerializer.Serialize<T>(ms, message, options: options);
            var result = ms.ToArray();
            ms.Dispose();
            return result;
        }
    }
}
