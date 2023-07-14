using KubeMQ.Contract.Interfaces;
using KubeMQ.Contract.Interfaces.Messages;
using System.Security.Cryptography;

namespace Encrypting
{
    internal class GlobalEncryptor : IGlobalMessageEncryptor
    {
        private const string KEY_NAME = "key";
        private const string IV_NAME = "iv";

        public Stream Decrypt(Stream stream, IMessageHeader headers)
        {
            var aes = Aes.Create();
            aes.Key=Convert.FromBase64String(headers[KEY_NAME]!);
            aes.IV=Convert.FromBase64String(headers[IV_NAME]!);
            return new CryptoStream(stream, aes.CreateDecryptor(), CryptoStreamMode.Read);
        }

        public byte[] Encrypt(byte[] data, out Dictionary<string, string> headers)
        {
            var aes = Aes.Create();
            headers = new Dictionary<string, string>()
            {
                {KEY_NAME,Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)) },
                {IV_NAME,Convert.ToBase64String(RandomNumberGenerator.GetBytes(16)) }
            };
            using (var ms = new MemoryStream())
            {
                using (var cstream = new CryptoStream(ms, aes.CreateEncryptor(Convert.FromBase64String(headers[KEY_NAME]), Convert.FromBase64String(headers[IV_NAME])), CryptoStreamMode.Write))
                {
                    cstream.Write(data, 0, data.Length);
                    cstream.Flush();
                }
                return ms.ToArray();
            }
        }
    }
}
