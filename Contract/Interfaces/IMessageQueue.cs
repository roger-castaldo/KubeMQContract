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
        IMessage<T>? Peek();
        IMessage<T>? Pop();
        IEnumerable<IMessage<T>> Pop(int count);
    }
}
