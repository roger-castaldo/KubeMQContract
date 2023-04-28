using KubeMQ.Contract.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace KubeMQ.Contract
{
    internal static class EncoderFactory
    {
        private static IEnumerable<string> loadedAssemblies = Array.Empty<string>();
        private static IEnumerable<object> converters = Array.Empty<object>();
        private static ReaderWriterLockSlim enumLock = new ReaderWriterLockSlim();

        public static void RegisterAssembly(ILogProvider logProvider, Assembly assembly)
        {
            enumLock.EnterWriteLock();
            if (!loadedAssemblies.Contains(assembly.FullName))
            {
                logProvider.LogTrace("Scanning Assembly {} for IMessageConverter implementations", assembly.FullName);
                converters = converters.Concat(assembly.GetTypes()
                                                       .Where(t => !t.IsInterface && !t.IsAbstract && t.GetInterfaces().Any(iface => iface.IsGenericType && iface.GetGenericTypeDefinition()==typeof(IMessageEncoder<>)))
                                                       .Select(t => Activator.CreateInstance(t))
                );
            }
            enumLock.ExitWriteLock();
        }

        private static IMessageEncoder<T>? LocateEncoder<T>()
        {
            enumLock.EnterReadLock();
            var encoder = (IMessageEncoder<T>?)converters.FirstOrDefault(obj => obj is  IMessageEncoder<T>);
            enumLock.ExitReadLock();
            return encoder??new JsonEncoder<T>();
        }
        
        public static byte[] Encode<T>(T message)
        {
            return LocateEncoder<T>().Encode(message);
        }

        public static T Decode<T>(Stream stream)
        {
            return LocateEncoder<T>().Decode(stream);
        }
    }
}
