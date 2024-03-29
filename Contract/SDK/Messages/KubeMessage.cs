﻿using Google.Protobuf.Collections;
using KubeMQ.Contract.SDK.Interfaces;

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
