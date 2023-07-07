using Google.Protobuf.Collections;
using Grpc.Core;
using KubeMQ.Contract.Attributes;
using KubeMQ.Contract.Interfaces;
using KubeMQ.Contract.Messages;
using KubeMQ.Contract.SDK.Grpc;
using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace KubeMQ.Contract
{
    internal static class Utility
    {
        internal static long ToUnixTime(DateTime timestamp)
        {
            return new DateTimeOffset(timestamp).ToUniversalTime().ToUnixTimeSeconds();
        }

        internal static DateTime FromUnixTime(long timestamp)
        {
            try
            {
                return DateTimeOffset.FromUnixTimeSeconds(timestamp).DateTime;
            }
            catch (Exception)
            {
                return DateTime.MaxValue;
            }
        }

        internal static string TypeName<T>()
        {
            return TypeName(typeof(T));
        }

        internal static string TypeName(Type type)
        {
            var result = type.Name;
            if (result.Contains('`'))
                result=result[..result.IndexOf('`')];
            return result;
        }
    }
}
