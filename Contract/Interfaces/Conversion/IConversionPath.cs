using KubeMQ.Contract.Interfaces.Messages;
using Microsoft.Extensions.Logging;

namespace KubeMQ.Contract.Interfaces.Conversion
{
    internal interface IConversionPath<T>
    {
        T? ConvertMessage(ILogger? logger, Stream stream,IMessageHeader messageHeader);
    }
}
