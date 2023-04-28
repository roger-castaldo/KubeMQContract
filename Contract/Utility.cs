using Grpc.Core;
using KubeMQ.Contract.Attributes;
using KubeMQ.Contract.Interfaces;
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
        public static string ComputeMessageGuid(Type messageType)
        {
            return $"{new Guid(MD5.Create().ComputeHash(UTF8Encoding.UTF8.GetBytes(messageType.FullName))).ToString()}";
        }

        public static void ConvertMessage<T>(T message,ConnectionOptions connectionOptions,out byte[] body,out string metaData)
        {
            body = EncoderFactory.Encode<T>(message);
            if (body.Length>connectionOptions.MaxBodySize)
            {
                using (var ms = new MemoryStream())
                {
                    var zip = new GZipStream(ms, System.IO.Compression.CompressionLevel.SmallestSize, false);
                    zip.Write(body, 0, body.Length);
                    zip.Flush();
                    body = ms.ToArray();
                    metaData = "C";
                }
            }
            else
                metaData="U";
            metaData+=$"-{typeof(T).GetCustomAttributes<MessageName>().Select(mn=>mn.Value).FirstOrDefault(typeof(T).Name)}-{typeof(T).GetCustomAttributes<MessageVersion>().Select(mc => mc.Version.ToString()).FirstOrDefault("0.0.0.0")}";
        }

        private static readonly Regex regMetaData = new Regex(@"^(U|C)-(.+)-((\d+\.)*(\d+))$", RegexOptions.Compiled);

        public static bool IsMessateTypeMatch(string metaData,Type t)
        {
            bool tmp;
            return IsMessageTypeMatch(metaData, t, out tmp);
        }

        private static bool IsMessageTypeMatch(string metaData,Type t,out bool isCompressed)
        {
            isCompressed=false;
            var match = regMetaData.Match(metaData);
            if (match.Success)
            {
                isCompressed=match.Groups[1].Value=="C";
                if (match.Groups[2].Value==t.GetCustomAttributes<MessageName>().Select(mn => mn.Value).FirstOrDefault(t.Name)
                    && new Version(match.Groups[3].Value)==new Version(t.GetCustomAttributes<MessageVersion>().Select(mc => mc.Version.ToString()).FirstOrDefault("0.0.0.0")))
                    return true;

            }
            else
                throw new InvalidDataException("MetaData is not valid");
            return false;
        }

        public static object? ConvertMessage(Type t,ILogProvider logProvider, string metaData, Google.Protobuf.ByteString body)
        {
            return typeof(Utility).GetMethod("convertMessage",BindingFlags.Static|BindingFlags.NonPublic)
                .MakeGenericMethod(new Type[] { t }).Invoke(null,new object[] { logProvider,metaData, body});
        }

        private static T? convertMessage<T>(ILogProvider logProvider,string? metaData, Google.Protobuf.ByteString body)
        {
            if (metaData==null)
                throw new ArgumentNullException(nameof(metaData));
            bool isCompressed;
            if (IsMessageTypeMatch(metaData, typeof(T), out isCompressed))
            {
                var stream = (isCompressed ? (Stream)new GZipStream(new MemoryStream(body.ToByteArray()), System.IO.Compression.CompressionMode.Decompress) : (Stream)new MemoryStream(body.ToByteArray()));
                return EncoderFactory.Decode<T>(stream);
            }
            else
                return ConverterFactory.ConvertMessage<T>(logProvider,metaData, body);
        }
        public static T? ConvertMessage<T>(ILogProvider logProvider, EventReceive msg)
        {
            return convertMessage<T>(logProvider,msg.Metadata,msg.Body);
        }

        public static T? ConvertMessage<T>(ILogProvider logProvider, Response msg)
        {
            return convertMessage<T>(logProvider, msg.Metadata, msg.Body);
        }

        public static T? ConvertMessage<T>(ILogProvider logProvider, Request msg)
        {
            return convertMessage<T>(logProvider, msg.Metadata, msg.Body);
        }

        public static T? ConvertMessage<T>(ILogProvider logProvider, QueueMessage msg)
        {
            return convertMessage<T>(logProvider, msg.Metadata, msg.Body);
        }

        public static long ToUnixTime(DateTime timestamp)
        {
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return (long)(timestamp.ToUniversalTime() - epoch).TotalSeconds;
        }
    }
}
