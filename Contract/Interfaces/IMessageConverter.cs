using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KubeMQ.Contract.Interfaces
{
    public interface IMessageConverter<T,V> 
    {
        V Convert(T source);
    }
}
