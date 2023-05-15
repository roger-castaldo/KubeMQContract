using Grpc.Core;
using KubeMQ.Contract.Interfaces;
using KubeMQ.Contract.SDK;
using KubeMQ.Contract.SDK.Grpc;
using Microsoft.Extensions.Logging;

namespace KubeMQ.Contract
{
    /// <summary>
    /// Houses the Connection Settings to use to connect to the particular instance of KubeMQ
    /// </summary>
    public class ConnectionOptions
    {
        /// <summary>
        /// The address and port to connection to.  This can be the dns name or an ip address.
        /// Use the format {ip/name}:{portnumber}.  Typically KubeMQ is configured to listen on 
        /// port 50000
        /// </summary>
        public string Address { get; init; } = "localhost:50000";
        /// <summary>
        /// The Unique Identification to be used when connecting to the KubeMQ server
        /// </summary>
        public string ClientId { get; init; } = Guid.NewGuid().ToString();
        /// <summary>
        /// The authentication token to use when connecting to the KubeMQ server
        /// </summary>
        public string AuthToken { get; init; } = string.Empty;
        /// <summary>
        /// The SSL Root certificate to use when connecting to the KubeMQ server
        /// </summary>
        public string SSLRootCertificate { get; init; } = string.Empty;
        /// <summary>
        /// The SSL Key to use when connecting to the KubeMQ server
        /// </summary>
        public string SSLKey { get; init; } = string.Empty;
        /// <summary>
        /// The SSL Certificat to use when connecting to the KubeMQ server
        /// </summary>
        public string SSLCertificate { get; init; } = string.Empty;
        /// <summary>
        /// Milliseconds to wait in between attempted reconnects to the KubeMQ server
        /// </summary>
        public int ReconnectInterval { get; init; } = 1000;
        /// <summary>
        /// The maximum body size in bytes configured on the KubeMQ server, default is 4096.
        /// If the encoded message exceeds the size, it will zip it in an attempt to transmit the 
        /// message.  If it still fails in size, an exception will be thrown.
        /// </summary>
        public int MaxBodySize { get; init; } = 4096;
        /// <summary>
        /// The ILogger instance to use for logging against any connections produced by these options.
        /// </summary>
        public ILogger? Logger { get; init; } = null;

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

        /// <summary>
        /// Called to use the Current Options to establish a connection to the KubeMQ server.
        /// </summary>
        /// <param name="globalMessageEncoder">If desired, an encoder can be specified here and will be used to encode message bodies as the default.  
        /// A type specific encoder can be specified to override this for that particular type of message.</param>
        /// <param name="globalMessageEncryptor">If desired, an encryptor can be specified here and will be used to secure the message bodies as the default.  
        /// A type specific encryptor can be specified to override this for that particular type of message.</param>
        /// <returns></returns>
        public IConnection EstablishConnection(IGlobalMessageEncoder? globalMessageEncoder=null,IGlobalMessageEncryptor? globalMessageEncryptor=null)
        {
            return new Connection(this,globalMessageEncoder,globalMessageEncryptor);
        }

        /// <summary>
        /// Called to use the Current Options to establish a Pub/Sub connection to the KubeMQ server.
        /// </summary>
        /// <param name="globalMessageEncoder">If desired, an encoder can be specified here and will be used to encode message bodies as the default.  
        /// A type specific encoder can be specified to override this for that particular type of message.</param>
        /// <param name="globalMessageEncryptor">If desired, an encryptor can be specified here and will be used to secure the message bodies as the default.  
        /// A type specific encryptor can be specified to override this for that particular type of message.</param>
        /// <returns></returns>
        public IPubSubConnection EstablishPubSubConnection(IGlobalMessageEncoder? globalMessageEncoder = null, IGlobalMessageEncryptor? globalMessageEncryptor = null)
        {
            return new Connection(this, globalMessageEncoder, globalMessageEncryptor);
        }

        /// <summary>
        /// Called to use the Current Options to establish a RPC connection to the KubeMQ server.
        /// </summary>
        /// <param name="globalMessageEncoder">If desired, an encoder can be specified here and will be used to encode message bodies as the default.  
        /// A type specific encoder can be specified to override this for that particular type of message.</param>
        /// <param name="globalMessageEncryptor">If desired, an encryptor can be specified here and will be used to secure the message bodies as the default.  
        /// A type specific encryptor can be specified to override this for that particular type of message.</param>
        /// <returns></returns>
        public IRPCConnection EstablishRPCConnection(IGlobalMessageEncoder? globalMessageEncoder = null, IGlobalMessageEncryptor? globalMessageEncryptor = null)
        {
            return new Connection(this, globalMessageEncoder, globalMessageEncryptor);
        }

        /// <summary>
        /// Called to use the Current Options to establish a Queue connection to the KubeMQ server.
        /// </summary>
        /// <param name="globalMessageEncoder">If desired, an encoder can be specified here and will be used to encode message bodies as the default.  
        /// A type specific encoder can be specified to override this for that particular type of message.</param>
        /// <param name="globalMessageEncryptor">If desired, an encryptor can be specified here and will be used to secure the message bodies as the default.  
        /// A type specific encryptor can be specified to override this for that particular type of message.</param>
        /// <returns></returns>
        public IQueueConnection EstablishQueueConnection(IGlobalMessageEncoder? globalMessageEncoder = null, IGlobalMessageEncryptor? globalMessageEncryptor = null)
        {
            return new Connection(this, globalMessageEncoder, globalMessageEncryptor);
        }
    }
}
