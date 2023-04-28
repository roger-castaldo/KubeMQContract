using KubeMQ.Contract.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace KubeMQ.Contract
{
    internal static class ConverterFactory
    {
        private static IEnumerable<string> loadedAssemblies = Array.Empty<string>();
        private static IEnumerable<object> converters = Array.Empty<object>();
        private static ReaderWriterLockSlim enumLock = new ReaderWriterLockSlim();

        public static void RegisterAssembly(ILogProvider logProvider,Assembly assembly)
        {
            enumLock.EnterWriteLock();
            if (!loadedAssemblies.Contains(assembly.FullName))
            {
                logProvider.LogTrace("Scanning Assembly {} for IMessageConverter implementations", assembly.FullName);
                converters = converters.Concat(assembly.GetTypes()
                                                       .Where(t => !t.IsInterface && !t.IsAbstract && t.GetInterfaces().Any(iface => iface.IsGenericType && iface.GetGenericTypeDefinition()==typeof(IMessageConverter<,>)))
                                                       .Select(t=>Activator.CreateInstance(t))
                );
            }
            enumLock.ExitWriteLock();
        }

        private static Type[] extractGenericArguements(Type t)
        {
            return t.GetInterfaces().FirstOrDefault(iface => iface.IsGenericType && iface.GetGenericTypeDefinition()==typeof(IMessageConverter<,>)).GetGenericArguments();
        }

        public static T? ConvertMessage<T>(ILogProvider logProvider, string metaData, Google.Protobuf.ByteString body)
        {
            logProvider.LogTrace("Attempting to convert Message {} to {}", metaData, typeof(T).Name);
            var found = false;
            var result = default(T?);
            enumLock.EnterReadLock();
            try
            {
                var converter = converters.FirstOrDefault(c => Utility.IsMessateTypeMatch(metaData, extractGenericArguements(c.GetType())[0])
                && extractGenericArguements(c.GetType())[1]==typeof(T));
                if (converter != null)
                {
                    logProvider.LogTrace("Found direct converter {} to convert {} to {}", converter.GetType().Name, metaData, typeof(T).Name);
                    found=true;
                    result = (T?)executeConverter(converter, Utility.ConvertMessage(extractGenericArguements(converter.GetType())[0],logProvider, metaData, body), typeof(T));
                }
                else
                {
                    converter = converters.FirstOrDefault(c => Utility.IsMessateTypeMatch(metaData, extractGenericArguements(c.GetType())[0]));
                    if (converter!=null)
                    {
                        logProvider.LogTrace("Attempting to convert {} to {} through converters for {}", metaData, typeof(T).Name, extractGenericArguements(converter.GetType())[0].Name);
                        var baseObject = Utility.ConvertMessage(extractGenericArguements(converter.GetType())[0],logProvider, metaData, body);
                        List<Type> testedTypes = new List<Type>() { baseObject.GetType() };
                        result = (T?)tryConverting(logProvider,ref testedTypes, baseObject, typeof(T), out found);
                    }
                }
            }
            catch(Exception)
            {}
            enumLock.ExitReadLock();
            if (!found)
                throw new InvalidCastException();
            return result;
        }

        private static object? tryConverting(ILogProvider logProvider,ref List<Type> testedTypes, object? source,Type destination, out bool found)
        {
            found=false;
            if (source==null)
                return null;
            var converter = converters.FirstOrDefault(c => extractGenericArguements(c.GetType())[0]==source.GetType()
            && extractGenericArguements(c.GetType())[1]==destination);
            if (converter!=null)
            {
                logProvider.LogTrace("Converting {} to destination {} through exact match", source.GetType().Name, destination.Name);
                found=true;
                return executeConverter(converter, source, destination);
            }
            else
            {
                foreach(var conv in converters.Where(c => extractGenericArguements(c.GetType())[0]==source.GetType()))
                {
                    if (!testedTypes.Contains(extractGenericArguements(conv.GetType())[1]))
                    {
                        testedTypes.Add(extractGenericArguements(conv.GetType())[1]);
                        logProvider.LogTrace("Attempting to convert {} to {} through {}", source.GetType().Name, destination.Name, extractGenericArguements(conv.GetType())[1].Name);
                        var result = tryConverting(logProvider,ref testedTypes, executeConverter(conv,source, extractGenericArguements(conv.GetType())[1]), destination, out found);
                        if (found)
                            return result;
                    }
                }
            }
            return null;
        }

        private static object? executeConverter(object converter, object? source,Type destination)
        {
            if (source==null)
                return null;
            return typeof(IMessageConverter<,>).MakeGenericType(source.GetType(),destination)
                .GetMethod("Convert")!
                .Invoke(converter,new object[] {source});
        }
    }
}
