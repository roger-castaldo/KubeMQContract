using KubeMQ.Contract.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KubeMQ.Contract.Factories
{
    internal class ConversionPath<T,V> : IConversionPath<V>
    {
        private readonly IEnumerable<object> path;
        private readonly IMessageEncoder<T> messageEncoder;
        private readonly IMessageEncryptor<T> messageEncryptor;

        public ConversionPath(IEnumerable<object> path,IEnumerable<Type> types)
        {
            this.path = path;
            messageEncoder = (IMessageEncoder<T>)Activator.CreateInstance(types
                .FirstOrDefault(type => type.GetInterfaces().Any(iface => iface==typeof(IMessageEncoder<T>)), typeof(JsonEncoder<T>))
                )!;
            messageEncryptor = (IMessageEncryptor<T>)Activator.CreateInstance(types
                .FirstOrDefault(type => type.GetInterfaces().Any(iface => iface==typeof(IMessageEncryptor<T>)), typeof(NonEncryptor<T>))
                )!;
        }

        public V? ConvertMessage(ILogProvider logProvider, Stream stream, IMessageHeader messageHeader)
        {
            object? result = messageEncoder.Decode((messageEncryptor==null ? stream : messageEncryptor.Decrypt(stream, messageHeader)));
            foreach (var converter in path)
            {
                logProvider.LogTrace("Attempting to convert {} to {} through converters for {}", typeof(T).Name, typeof(V).Name, extractGenericArguements(converter.GetType())[0].Name);
                result = executeConverter(converter, result, extractGenericArguements(converter.GetType())[1]);
            }
            return (V?)result;
        }

        private static Type[] extractGenericArguements(Type t)
        {
            return t.GetInterfaces().First(iface => iface.IsGenericType && iface.GetGenericTypeDefinition()==typeof(IMessageConverter<,>)).GetGenericArguments();
        }

        private static object? executeConverter(object converter, object? source, Type destination)
        {
            if (source==null)
                return null;
            return typeof(IMessageConverter<,>).MakeGenericType(source.GetType(), destination)
                .GetMethod("Convert")!
                .Invoke(converter, new object[] { source });
        }
    }
}
