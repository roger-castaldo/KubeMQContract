using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KubeMQ.Contract.Interfaces
{
    public interface IMessageEncryptor<T>
    {
        Stream Decrypt(Stream stream, IMessageHeader headers);
        byte[] Encrypt(Stream stream, out Dictionary<string, string> headers);
    }
}
