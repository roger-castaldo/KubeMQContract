using Grpc.Core;
using KubeMQ.Contract.Interfaces;
using KubeMQ.Contract.SDK.Grpc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KubeMQ.Contract
{
    public class ConnectionOptions
    {
        public string Address { get; init; } = "localhost:50000";
        public string ClientId { get; } = Guid.NewGuid().ToString();
        public double CallTimeout { get; init; } = 0;
        public string AuthToken { get; init; } = string.Empty;
        public string SSLRootCertificate { get; init; } = string.Empty;
        public string SSLKey { get; init; } = string.Empty;
        public string SSLCertificate { get; init; } = string.Empty;
        public int ReconnectInterval { get; init; } = 1000;
        public int MaxBodySize { get; init; } = 4096;

        internal SslCredentials? SSLCredentials
        {
            get
            {
                if (string.IsNullOrEmpty(SSLCertificate))
                    return null;
                if (!string.IsNullOrEmpty(SSLCertificate) && !string.IsNullOrEmpty(SSLKey))
                    return new SslCredentials(SSLRootCertificate, new KeyCertificatePair(SSLCertificate, SSLKey));
                else
                    return new SslCredentials(SSLRootCertificate);
            }
        }

        internal Metadata GrpcMetadata
        {
            get
            {
                var result = new Metadata();
                if (!string.IsNullOrEmpty(AuthToken))
                    result.Add(new Metadata.Entry("authorization", AuthToken));
                return result;
            }
        }

        internal CallOptions CallOptions
        {
            get
            {
                var result = new CallOptions();
                if (CallTimeout > 0)
                    result = result.WithDeadline(DateTime.Now.AddMilliseconds(CallTimeout));
                return result;
            }
        }


        public IConnection EstablishConnection()
        {
            return new Connection(this);
        }
    }
}
