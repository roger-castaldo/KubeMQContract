using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KubeMQ.Contract.Interfaces
{
    public interface IMessageQueue<T>
    {
        Guid ID { get; }
        bool HasMore { get; }
        T? Peek();
        T? Pop();
        IEnumerable<T> Pop(int count);
    }
}
