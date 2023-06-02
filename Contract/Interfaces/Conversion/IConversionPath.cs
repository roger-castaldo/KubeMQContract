using KubeMQ.Contract.Interfaces.Messages;

namespace KubeMQ.Contract.Interfaces.Conversion
{
    internal interface IConversionPath<T>
    {
        T? ConvertMessage(ILogProvider logProvider, Stream stream,IMessageHeader messageHeader);
    }
}
