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

namespace KubeMQ.Contract.SDK
{
    internal class KubeMessage<T> : IKubeMessage
    {
        public string ID => Guid.NewGuid().ToString();

        private readonly string _metaData;
        public string MetaData => _metaData;

        private readonly string _channel;
        public string Channel => _channel;

        private readonly string _clientID;
        public string ClientID => _clientID;

        private readonly byte[] _body;
        public byte[] Body => _body;

        private readonly MapField<string, string> _tags;
        public MapField<string, string> Tags => _tags;

        public KubeMessage(T message,ConnectionOptions connectionOptions,string? channel=null)
        {
            _tags = new MapField<string, string>();
            _clientID=connectionOptions.ClientId;
            _channel = channel??typeof(T).GetCustomAttributes<MessageChannel>().Select(mc => mc.Name).FirstOrDefault(String.Empty);
            if (string.IsNullOrEmpty(_channel))
                throw new ArgumentNullException(nameof(Channel), "message must have a channel value");
            Utility.ConvertMessage<T>(message,connectionOptions, out _body, out _metaData);
            if (_body.Length>connectionOptions.MaxBodySize)
                throw new ArgumentOutOfRangeException(nameof(message),"message data exceeds maxmium message size");
        }
    }
}
