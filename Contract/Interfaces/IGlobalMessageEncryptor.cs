using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KubeMQ.Contract.Interfaces
{
    public interface IGlobalMessageEncryptor
    {
        Stream Decrypt(Stream stream, IMessageHeader headers);
        byte[] Encrypt(byte[] data, out Dictionary<string, string> headers);
    }
}
