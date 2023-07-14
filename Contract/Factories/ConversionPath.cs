using KubeMQ.Contract.Interfaces;
using KubeMQ.Contract.Interfaces.Conversion;
using KubeMQ.Contract.Interfaces.Messages;
using Microsoft.Extensions.Logging;

namespace KubeMQ.Contract.Factories
{
    internal class ConversionPath<T,V> : IConversionPath<V> 
    {
        private readonly IEnumerable<object> path;
        private readonly IMessageEncoder<T> messageEncoder;
        private readonly IMessageEncryptor<T> messageEncryptor;
        private readonly IGlobalMessageEncoder? globalMessageEncoder;
        private readonly IGlobalMessageEncryptor? globalMessageEncryptor;

        public ConversionPath(IEnumerable<object> path,IEnumerable<Type> types, IGlobalMessageEncoder? globalMessageEncoder, IGlobalMessageEncryptor? globalMessageEncryptor)
        {
            this.path = path;
            this.globalMessageEncoder = globalMessageEncoder;
            this.globalMessageEncryptor = globalMessageEncryptor;
            messageEncoder = (IMessageEncoder<T>)Activator.CreateInstance(types
                .FirstOrDefault(type => type.GetInterfaces().Any(iface => iface==typeof(IMessageEncoder<T>)), typeof(JsonEncoder<T>))
                )!;
            messageEncryptor = (IMessageEncryptor<T>)Activator.CreateInstance(types
                .FirstOrDefault(type => type.GetInterfaces().Any(iface => iface==typeof(IMessageEncryptor<T>)), typeof(NonEncryptor<T>))
                )!;
        }

        public V? ConvertMessage(ILogger? logger, Stream stream, IMessageHeader messageHeader)
        {
            if (globalMessageEncryptor!=null && messageEncryptor is NonEncryptor<T>)
                stream=globalMessageEncryptor.Decrypt(stream, messageHeader);
            else
                stream = messageEncryptor.Decrypt(stream, messageHeader);
            object? result;
            if (globalMessageEncoder!=null && messageEncoder is JsonEncoder<T>)
                result =  globalMessageEncoder.Decode<T>(stream);
            else
                result = messageEncoder.Decode(stream);
            foreach (var converter in path)
            {
                logger?.LogTrace("Attempting to convert {} to {} through converters for {}", Utility.TypeName<T>(), Utility.TypeName<V>(), Utility.TypeName(ExtractGenericArguements(converter.GetType())[0]));
                result = ExecuteConverter(converter, result, ExtractGenericArguements(converter.GetType())[1]);
            }
            return (V?)result;
        }

        private static Type[] ExtractGenericArguements(Type t) => t.GetInterfaces().First(iface => iface.IsGenericType && iface.GetGenericTypeDefinition()==typeof(IMessageConverter<,>)).GetGenericArguments();

        private static object? ExecuteConverter(object converter, object? source, Type destination)
        {
            if (source==null)
                return null;
            return typeof(IMessageConverter<,>).MakeGenericType(source.GetType(), destination)
                .GetMethod("Convert")!
                .Invoke(converter, new object[] { source });
        }
    }
}
