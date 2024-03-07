using KubeMQ.Contract.Interfaces;
using KubeMQ.Contract.Interfaces.Conversion;
using KubeMQ.Contract.Interfaces.Messages;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace KubeMQ.Contract.Factories
{
    internal class ConversionPath<T,V> : IConversionPath<V> 
    {
        private readonly IEnumerable<object> path;
        private readonly IMessageTypeEncoder<T> messageEncoder;
        private readonly IMessageTypeEncryptor<T> messageEncryptor;
        private readonly IMessageEncoder? globalMessageEncoder;
        private readonly IMessageEncryptor? globalMessageEncryptor;

        public ConversionPath(IEnumerable<object> path,IEnumerable<Type> types, IMessageEncoder? globalMessageEncoder, IMessageEncryptor? globalMessageEncryptor,IServiceProvider? serviceProvider)
        {
            this.path = path;
            this.globalMessageEncoder = globalMessageEncoder;
            this.globalMessageEncryptor = globalMessageEncryptor;
            var encoderType = types
                .FirstOrDefault(type => type.GetInterfaces().Any(iface => iface==typeof(IMessageTypeEncoder<T>)), typeof(JsonEncoder<T>));
            var encryptorType = types
                .FirstOrDefault(type => type.GetInterfaces().Any(iface => iface==typeof(IMessageTypeEncryptor<T>)), typeof(NonEncryptor<T>));
            if (serviceProvider!=null)
            {
                messageEncoder = (IMessageTypeEncoder<T>)ActivatorUtilities.CreateInstance(serviceProvider, encoderType, Array.Empty<object>());
                messageEncryptor = (IMessageTypeEncryptor<T>)ActivatorUtilities.CreateInstance(serviceProvider, encryptorType, Array.Empty<object>());
            }
            else
            {
                messageEncoder = (IMessageTypeEncoder<T>)Activator.CreateInstance(encoderType)!;
                messageEncryptor = (IMessageTypeEncryptor<T>)Activator.CreateInstance(encryptorType)!;
            }
        }

        public V? ConvertMessage(ILogger? logger, Stream stream, IMessageHeader messageHeader)
        {
            stream = (globalMessageEncryptor!=null && messageEncryptor is NonEncryptor<T> ? globalMessageEncryptor : messageEncryptor).Decrypt(stream, messageHeader);
            object? result = (globalMessageEncoder!=null && messageEncoder is JsonEncoder<T>? globalMessageEncoder.Decode<T>(stream):messageEncoder.Decode(stream));
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

        public bool CanConvert(Type sourceType)
            => sourceType==typeof(T);
    }
}
