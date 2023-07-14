using KubeMQ.Contract.Interfaces.Messages;

namespace KubeMQ.Contract.Factories
{
    internal class NonEncryptor<T> : IMessageEncryptor<T> 
    {
        public Stream Decrypt(Stream data, IMessageHeader headers)
        {
            return data;
        }

        public byte[] Encrypt(byte[] data, out Dictionary<string, string> headers)
        {
            headers = new Dictionary<string, string>();
            return data;
        }
    }
}
