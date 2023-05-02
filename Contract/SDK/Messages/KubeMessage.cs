using Google.Protobuf;
using Google.Protobuf.Collections;
using Grpc.Core;
using KubeMQ.Contract.Attributes;
using KubeMQ.Contract.SDK.Interfaces;
using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace KubeMQ.Contract.SDK.Messages
{
    internal class KubeMessage : IKubeMessage
    {
        public string ID { get; init; } = Guid.NewGuid().ToString();
        public string MetaData { get; init; } = string.Empty;
        public string Channel { get; init; } = string.Empty;
        public string ClientID { get; init; } = string.Empty;
        public byte[] Body { get; init; } = Array.Empty<byte>();
        public MapField<string, string> Tags { get; init; } = new MapField<string, string>();
    }
}
