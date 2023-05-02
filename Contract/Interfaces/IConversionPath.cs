using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KubeMQ.Contract.Interfaces
{
    internal interface IConversionPath<T>
    {
        T? ConvertMessage(ILogProvider logProvider, Stream stream,IMessageHeader messageHeader);
    }
}
