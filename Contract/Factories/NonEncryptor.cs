using KubeMQ.Contract.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
