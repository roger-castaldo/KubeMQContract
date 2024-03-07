using Grpc.Core;
using KubeMQ.Contract.Interfaces;
using KubeMQ.Contract.Interfaces.Connections;
using KubeMQ.Contract.SDK.Connection;
using Microsoft.Extensions.Logging;

namespace KubeMQ.Contract
{
    /// <summary>
    /// Houses the Connection Settings to use to connect to the particular instance of KubeMQ
    /// </summary>
    public class ConnectionOptions
    {
        private const int DEFAULT_MAX_SIZE = 1024 * 1024 * 100; // 100MB
        /// <summary>
        /// The address and port to connection to.  This can be the dns name or an ip address.
        /// Use the format {ip/name}:{portnumber}.  Typically KubeMQ is configured to listen on 
        /// port 50000
        /// </summary>
        public string Address { get; init; } = "http://localhost:50000";
        /// <summary>
        /// The Unique Identification to be used when connecting to the KubeMQ server
        /// </summary>
        public string ClientId { get; init; } = string.Empty;
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
        /// The maximum body size in bytes configured on the KubeMQ server, default is 100MB.
        /// If the encoded message exceeds the size, it will zip it in an attempt to transmit the 
        /// message.  If it still fails in size, an exception will be thrown.
        /// </summary>
        public int MaxBodySize { get; init; } = DEFAULT_MAX_SIZE;
        /// <summary>
        /// The ILoggerProvider instance to use for logging against any connections produced by these options.
        /// </summary>
        public ILoggerProvider? Logger { get; init; } = null;
        /// <summary>
        /// The IServiceProvider instance that can be used for dependency injection when constructing encryptors and converters if needed
        /// </summary>
        public IServiceProvider? ServiceProvider { get; init; } = null;

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
        public IConnection EstablishConnection(IMessageEncoder? globalMessageEncoder=null,IMessageEncryptor? globalMessageEncryptor=null)
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
        public IPubSubConnection EstablishPubSubConnection(IMessageEncoder? globalMessageEncoder = null, IMessageEncryptor? globalMessageEncryptor = null)
        {
            return new Connection(this, globalMessageEncoder, globalMessageEncryptor);
        }

        /// <summary>
        /// Called to use the Current Options to establish a Pub/Sub Stream connection to the KubeMQ server.
        /// </summary>
        /// <param name="globalMessageEncoder">If desired, an encoder can be specified here and will be used to encode message bodies as the default.  
        /// A type specific encoder can be specified to override this for that particular type of message.</param>
        /// <param name="globalMessageEncryptor">If desired, an encryptor can be specified here and will be used to secure the message bodies as the default.  
        /// A type specific encryptor can be specified to override this for that particular type of message.</param>
        /// <returns></returns>
        public IPubSubStreamConnection EstablishPubSubStreamConnection(IMessageEncoder? globalMessageEncoder = null, IMessageEncryptor? globalMessageEncryptor = null)
        {
            return new Connection(this, globalMessageEncoder, globalMessageEncryptor);
        }

        /// <summary>
        /// Called to use the Current Options to establish a RPC Query connection to the KubeMQ server.
        /// </summary>
        /// <param name="globalMessageEncoder">If desired, an encoder can be specified here and will be used to encode message bodies as the default.  
        /// A type specific encoder can be specified to override this for that particular type of message.</param>
        /// <param name="globalMessageEncryptor">If desired, an encryptor can be specified here and will be used to secure the message bodies as the default.  
        /// A type specific encryptor can be specified to override this for that particular type of message.</param>
        /// <returns></returns>
        public IRPCQueryConnection EstablishRPCQueryConnection(IMessageEncoder? globalMessageEncoder = null, IMessageEncryptor? globalMessageEncryptor = null)
        {
            return new Connection(this, globalMessageEncoder, globalMessageEncryptor);
        }

        /// <summary>
        /// Called to use the Current Options to establish a RPC Command connection to the KubeMQ server.
        /// </summary>
        /// <param name="globalMessageEncoder">If desired, an encoder can be specified here and will be used to encode message bodies as the default.  
        /// A type specific encoder can be specified to override this for that particular type of message.</param>
        /// <param name="globalMessageEncryptor">If desired, an encryptor can be specified here and will be used to secure the message bodies as the default.  
        /// A type specific encryptor can be specified to override this for that particular type of message.</param>
        /// <returns></returns>
        public IRPCCommandConnection EstablishRPCCommandConnection(IMessageEncoder? globalMessageEncoder = null, IMessageEncryptor? globalMessageEncryptor = null)
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
        public IQueueConnection EstablishQueueConnection(IMessageEncoder? globalMessageEncoder = null, IMessageEncryptor? globalMessageEncryptor = null)
        {
            return new Connection(this, globalMessageEncoder, globalMessageEncryptor);
        }
    }
}
