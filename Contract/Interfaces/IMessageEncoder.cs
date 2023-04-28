using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KubeMQ.Contract.Interfaces
{
    public interface IMessageEncoder<T>
    {
        byte[] Encode(T message);
        T? Decode(Stream stream);
    }
}
