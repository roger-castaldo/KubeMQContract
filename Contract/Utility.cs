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
        private static readonly DateTime EPOCH = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public static long ToUnixTime(DateTime timestamp)
        {
            return (long)(timestamp.ToUniversalTime() - EPOCH).TotalSeconds;
        }

        internal static DateTime FromUnixTime(long serverStartTime)
        {
            return EPOCH.AddSeconds(serverStartTime);
        }
    }
}
