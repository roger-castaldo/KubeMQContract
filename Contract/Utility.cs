using Grpc.Core;
using KubeMQ.Contract.Attributes;
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
            body = System.Text.UTF8Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize<T>(message, new System.Text.Json.JsonSerializerOptions()
            {
                WriteIndented=false
            }));
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
            metaData+=$"-{typeof(T).Name}-{typeof(T).GetCustomAttributes<MessageVersion>().Select(mc => mc.Version.ToString()).FirstOrDefault("0.0.0.0")}";
        }

        private static readonly Regex regMetaData = new Regex(@"^(U|C)-([^-]+)-((\d+\.)*(\d+))$", RegexOptions.Compiled);

        public static T? ConvertMessage<T>(EventReceive msg)
        {
            if (msg.Metadata==null)
                throw new ArgumentNullException(nameof(msg.Metadata));
            var match = regMetaData.Match(msg.Metadata);
            if (match.Success){
                var stream = (match.Groups[1].Value=="C" ? (Stream)new GZipStream(new MemoryStream(msg.Body.ToByteArray()), System.IO.Compression.CompressionLevel.SmallestSize) : (Stream)new MemoryStream(msg.Body.ToByteArray()));
                if (match.Groups[2].Value!=typeof(T).Name
                    || new Version(match.Groups[3].Value)!=new Version(typeof(T).GetCustomAttributes<MessageVersion>().Select(mc => mc.Version.ToString()).FirstOrDefault("0.0.0.0")))
                    throw new InvalidCastException();
                return System.Text.Json.JsonSerializer.Deserialize<T>(stream);
            }else
                throw new InvalidDataException("MetaData is not valid");
        }
    }
}
