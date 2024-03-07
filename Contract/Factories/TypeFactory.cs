using Google.Protobuf;
using Google.Protobuf.Collections;
using KubeMQ.Contract.Attributes;
using KubeMQ.Contract.Interfaces;
using KubeMQ.Contract.Interfaces.Conversion;
using KubeMQ.Contract.Interfaces.Messages;
using KubeMQ.Contract.Messages;
using KubeMQ.Contract.SDK.Grpc;
using KubeMQ.Contract.SDK.Interfaces;
using KubeMQ.Contract.SDK.Messages;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.RegularExpressions;
using static KubeMQ.Contract.SDK.Grpc.Request.Types;

namespace KubeMQ.Contract.Factories
{
    internal class TypeFactory<T>:IMessageFactory<T>,IConversionPath<T>,ITypeFactory
    {
        private static readonly Regex regMetaData = new(@"^(U|C)-(.+)-((\d+\.)*(\d+))$", RegexOptions.Compiled,TimeSpan.FromMilliseconds(200));
        private static readonly Regex headerRegex = new("\r\n([^:]+):\\s*([^\r]+)\r\n", RegexOptions.Compiled|RegexOptions.ECMAScript, TimeSpan.FromMilliseconds(200));

        private readonly IMessageEncoder? globalMessageEncoder;
        private readonly IMessageEncryptor? globalMessageEncryptor;
        private readonly IMessageTypeEncoder<T> messageEncoder;
        private readonly IMessageTypeEncryptor<T> messageEncryptor;
        private readonly IEnumerable<IConversionPath<T>> converters;

        public bool IgnoreMessageHeader { get; private init; }

        private readonly string messageName = typeof(T).GetCustomAttributes<MessageName>().Select(mn => mn.Value).FirstOrDefault(Utility.TypeName<T>());
        private readonly string messageVersion = typeof(T).GetCustomAttributes<MessageVersion>().Select(mc => mc.Version.ToString()).FirstOrDefault("0.0.0.0");
        private readonly string messageChannel = typeof(T).GetCustomAttributes<MessageChannel>().Select(mc => mc.Name).FirstOrDefault(string.Empty);
        private readonly bool stored = typeof(T).GetCustomAttributes<StoredMessage>().FirstOrDefault() != null;
        private readonly int requestTimeout = typeof(T).GetCustomAttributes<MessageResponseTimeout>().Select(mrt => mrt.Value).FirstOrDefault(5000);

        public TypeFactory(IMessageEncoder? globalMessageEncoder, IMessageEncryptor? globalMessageEncryptor,IServiceProvider? serviceProvider, bool ignoreMessageHeader)
        {
            this.globalMessageEncoder = globalMessageEncoder;
            this.globalMessageEncryptor = globalMessageEncryptor;
            IgnoreMessageHeader=ignoreMessageHeader;
            var types = AssemblyLoadContext.All
                .SelectMany(context => context.Assemblies)
                .SelectMany(assembly =>
                {
                    try
                    {
                        return assembly.GetTypes()
                        .Where(t => !t.IsInterface && !t.IsAbstract 
                            && t.GetInterfaces().Any(iface => iface==typeof(IMessageTypeEncoder<T>) 
                                || iface==typeof(IMessageTypeEncryptor<T>) 
                                || (iface.IsGenericType && iface.GetGenericTypeDefinition()==typeof(IMessageConverter<,>))));
                    }
                    catch (Exception)
                    {
                        return Array.Empty<Type>();
                    }
                });
            var encoderType = types
                .FirstOrDefault(type => type.GetInterfaces().Any(iface => iface==typeof(IMessageTypeEncoder<T>)), typeof(JsonEncoder<T>));
            var encryptorType = types
                .FirstOrDefault(type => type.GetInterfaces().Any(iface => iface==typeof(IMessageTypeEncryptor<T>)), typeof(NonEncryptor<T>));
            if (serviceProvider!=null)
            {
                messageEncoder = (IMessageTypeEncoder<T>)ActivatorUtilities.CreateInstance(serviceProvider, encoderType, Array.Empty<object>());
                messageEncryptor = (IMessageTypeEncryptor<T>)ActivatorUtilities.CreateInstance(serviceProvider, encryptorType, Array.Empty<object>());
            }
            else
            {
                messageEncoder = (IMessageTypeEncoder<T>)Activator.CreateInstance(encoderType)!;
                messageEncryptor = (IMessageTypeEncryptor<T>)Activator.CreateInstance(encryptorType)!;
            }
            converters = (IgnoreMessageHeader
                ? Array.Empty<IConversionPath<T>>() 
                : TraceConverters(typeof(T), globalMessageEncoder, globalMessageEncryptor, types, Array.Empty<object>(), Array.Empty<IConversionPath<T>>(),serviceProvider));
        }

        private static IEnumerable<IConversionPath<T>> TraceConverters(Type destinationType, IMessageEncoder? globalMessageEncoder, IMessageEncryptor? globalMessageEncryptor, IEnumerable<Type> types, IEnumerable<object> curPath, IEnumerable<IConversionPath<T>> converters, IServiceProvider? serviceProvider)
        {
            var subPaths = types.Where(t => t.GetInterfaces().Any(iface => iface.IsGenericType &&
                iface.GetGenericTypeDefinition()==typeof(IMessageConverter<,>)
                && iface.GetGenericArguments()[1]==destinationType
                && !converters.Any(conv => conv.GetType().GetGenericArguments()[0]==iface.GetGenericArguments()[0]))
            )
                .Select(t => curPath.Prepend((serviceProvider==null ? Activator.CreateInstance(t)! : ActivatorUtilities.CreateInstance(serviceProvider, t, Array.Empty<object>()))));

            var results = converters.Concat(
                subPaths.Select(path =>
                {
                    var args = new object[] { path, types, globalMessageEncoder, globalMessageEncryptor, serviceProvider };
                    var type = typeof(ConversionPath<,>).MakeGenericType(new Type[] {
                        ExtractGenericArguements(path.First().GetType())[0],
                        typeof(T)
                    });
                    return (IConversionPath<T>)(serviceProvider==null ? Activator.CreateInstance(type, args)! : ActivatorUtilities.CreateInstance(serviceProvider, type, args));
                })
            );

            foreach (var path in subPaths)
                results = TraceConverters(ExtractGenericArguements(path.First().GetType())[0], globalMessageEncoder, globalMessageEncryptor, types, path, results,serviceProvider);

            return results;
        }

        private static Type[] ExtractGenericArguements(Type t)
            => t.GetInterfaces().First(iface => iface.IsGenericType && iface.GetGenericTypeDefinition()==typeof(IMessageConverter<,>)).GetGenericArguments();

        private static bool IsMessageTypeMatch(string metaData, Type t, out bool isCompressed)
        {
            isCompressed=false;
            var match = regMetaData.Match(metaData);
            if (!match.Success)
            {
                var headerKey = t.GetCustomAttribute<UsesHttpSource>()?.MessageTypeHeader;
                if (headerKey!=null)
                {
                    var m = headerRegex.Matches(metaData).FirstOrDefault(m => m.Groups[1].Value.Equals(headerKey, StringComparison.InvariantCultureIgnoreCase));
                    if (m!=null)
                        match=regMetaData.Match(m.Groups[2].Value.Trim());
                }
            }
            if (match.Success)
            {
                isCompressed=match.Groups[1].Value=="C";
                if (match.Groups[2].Value==t.GetCustomAttributes<MessageName>().Select(mn => mn.Value).FirstOrDefault(Utility.TypeName(t))
                    && new Version(match.Groups[3].Value)==new Version(t.GetCustomAttributes<MessageVersion>().Select(mc => mc.Version.ToString()).FirstOrDefault("0.0.0.0")))
                    return true;

            }
            else
                throw new InvalidDataException("MetaData is not valid");
            return false;
        }

        private T ConvertData(ILogger? logger, string metaData, ByteString body, MapField<string, string>? tags)
        {
            if (!IgnoreMessageHeader && metaData==null)
                throw new ArgumentNullException(nameof(metaData));
            IConversionPath<T>? converter = null;
            bool compressed = false;
            var headerKey = typeof(T).GetCustomAttribute<UsesHttpSource>()?.MessageTypeHeader;
            if (IgnoreMessageHeader || IsMessageTypeMatch(metaData, typeof(T), out compressed))
                converter = this;
            else
            {
                foreach (var conv in converters)
                {
                    if (IsMessageTypeMatch(metaData, conv.GetType().GetGenericArguments()[0], out compressed))
                    {
                        headerKey=conv.GetType().GetGenericArguments()[0].GetCustomAttribute<UsesHttpSource>()?.MessageTypeHeader;
                        converter=conv;
                        break;
                    }
                }
            }
            if (converter==null)
                throw new InvalidCastException();
            var stream = (compressed ? (Stream)new GZipStream(new MemoryStream(body.ToByteArray()), System.IO.Compression.CompressionMode.Decompress) : (Stream)new MemoryStream(body.ToByteArray()));
            var result = converter.ConvertMessage(logger, stream, new MessageHeaders(
                (metaData==null ? Array.Empty<KeyValuePair<string,string>>() : 
                    headerRegex.Matches(metaData)
                    .Where(m => headerKey==null || !m.Groups[1].Value.Equals(headerKey,StringComparison.InvariantCultureIgnoreCase))
                    .Select(m=>new KeyValuePair<string, string>(m.Groups[1].Value, m.Groups[2].Value))
                ),
                tags));
            if (result==null)
                throw new NullReferenceException(nameof(result));
            return result;
        }

        private Interfaces.Messages.IInternalMessage<T> ConvertMessage(ILogger? logger, string metaData, ByteString body, MapField<string, string>? tags,string id,DateTime timestamp)
        {
            try
            {
                return new Message<T>(id, ConvertData(logger, metaData, body, tags), timestamp: timestamp, tags: tags);
            }catch (Exception e)
            {
                logger?.LogError("Message Conversion Error: {@Error}", e);
                return new Message<T>(id,timestamp: timestamp, tags: tags, exception:e);
            }
        }

        T? IConversionPath<T>.ConvertMessage(ILogger? logger, Stream stream, IMessageHeader messageHeader)
        {
            stream = (globalMessageEncryptor!=null && messageEncryptor is NonEncryptor<T>? globalMessageEncryptor : messageEncryptor).Decrypt(stream,messageHeader);
            return (globalMessageEncoder!=null && messageEncoder is JsonEncoder<T> 
                ? globalMessageEncoder.Decode<T>(stream)
                : messageEncoder.Decode(stream));
        }

        private IKubeMessage ProduceBaseMessage(T message, ConnectionOptions connectionOptions,string clientID, string? channel, Dictionary<string, string>? tagCollection)
        {
            channel ??= messageChannel;
            if (string.IsNullOrEmpty(channel))
                throw new ArgumentNullException(nameof(channel), "message must have a channel value");

            var body = (globalMessageEncryptor!=null && messageEncryptor is NonEncryptor<T>  ? globalMessageEncryptor : messageEncryptor).Encrypt(messageEncoder.Encode(message),out var messageHeaders);

            var metaData = string.Empty;
            if (body.Length>(connectionOptions.MaxBodySize==0?int.MaxValue : connectionOptions.MaxBodySize))
            {
                using var ms = new MemoryStream();
                var zip = new GZipStream(ms, System.IO.Compression.CompressionLevel.SmallestSize, false);
                zip.Write(body, 0, body.Length);
                zip.Flush();
                body = ms.ToArray();
                metaData = "C";
            }
            else
                metaData="U";
            metaData+=$"-{messageName}-{messageVersion}";

            var tags = new MapField<string, string>();
            if (messageHeaders!=null)
            {
                foreach (var tag in messageHeaders)
                    tags.Add(tag.Key, tag.Value);
            }
            if (tagCollection!=null)
            {
                foreach (var tag in tagCollection.Where(t=>!tags.ContainsKey(t.Key)))
                    tags.Add(tag.Key, tag.Value);
            }

            if (body.Length > (connectionOptions.MaxBodySize==0 ? int.MaxValue : connectionOptions.MaxBodySize))
                throw new ArgumentOutOfRangeException(nameof(message), "message data exceeds maxmium message size");


            return new KubeMessage()
            {
                ClientID=clientID,
                Channel=channel,
                Body=body,
                MetaData=metaData,
                Tags=tags
            };
        }

        private static void ExtractQueuePolicy(Type type, ref int? expirationSeconds, ref int? maxCount, ref string? maxCountChannel)
        {
            var policy = typeof(T).GetCustomAttributes<MessageQueuePolicy>().FirstOrDefault();

            if (policy!=null)
            {
                expirationSeconds ??= policy.ExpirationSeconds;
                maxCount ??= policy.MaxCount;
                maxCountChannel ??= policy.MaxCountChannel;
            }

            if ((maxCount!=null && maxCountChannel==null)
                ||(maxCount==null&&maxCountChannel!=null))
                throw new ArgumentOutOfRangeException(nameof(maxCountChannel), $"You must specify both the {nameof(maxCount)} and {nameof(maxCountChannel)} if you are specifying either");
        }

        IKubeEnqueue IMessageFactory<T>.Enqueue(T message, ConnectionOptions connectionOptions, string clientID, string? channel, Dictionary<string, string>? tagCollection, int? delaySeconds, int? expirationSeconds, int? maxCount, string? maxCountChannel)
        {
            ExtractQueuePolicy(typeof(T), ref expirationSeconds, ref maxCount, ref maxCountChannel);
            
            return new KubeEnqueue(ProduceBaseMessage(message, connectionOptions,clientID, channel, tagCollection)){
                DelaySeconds=delaySeconds,
                ExpirationSeconds=expirationSeconds,
                MaxSize=maxCount,
                MaxCountChannel = maxCountChannel 
            };
        }

        IKubeBatchEnqueue IMessageFactory<T>.Enqueue(IEnumerable<T> messages, ConnectionOptions connectionOptions, string clientID, string? channel, Dictionary<string, string>? tagCollection, int? delaySeconds, int? expirationSeconds, int? maxCount, string? maxCountChannel)
        {
            ExtractQueuePolicy(typeof(T), ref expirationSeconds, ref maxCount, ref maxCountChannel);

            return new KubeBatchEnqueue(){
                Messages=messages.Select(message =>
                {
                    var msg = new KubeEnqueue(ProduceBaseMessage(message, connectionOptions,clientID, channel, tagCollection))
                    {
                        DelaySeconds=delaySeconds,
                        ExpirationSeconds=expirationSeconds,
                        MaxSize=maxCount,
                        MaxCountChannel = maxCountChannel
                    };
                    return new QueueMessage()
                    {
                        MessageID= msg.ID.ToString(),
                        ClientID = msg.ClientID,
                        Channel = msg.Channel,
                        Metadata = msg.MetaData,
                        Body = ByteString.CopyFrom(msg.Body),
                        Tags = { msg.Tags },
                        Policy = msg.Policy
                    };
                })
            };
        }

        IKubeEvent IMessageFactory<T>.Event(T message, ConnectionOptions connectionOptions, string clientID, string? channel, Dictionary<string, string>? tagCollection)
            => new KubeEvent(ProduceBaseMessage(message, connectionOptions,clientID, channel, tagCollection))
            {
                Stored=stored
            };

        IKubeRequest IMessageFactory<T>.Request(T message, ConnectionOptions connectionOptions, string clientID, string? channel, Dictionary<string, string>? tagCollection, int? timeout,RequestType requestType)
            => new KubeRequest(ProduceBaseMessage(message, connectionOptions,clientID, channel, tagCollection))
            {
                Timeout=timeout??requestTimeout,
                CommandType=requestType
            };

        IKubeMessage IMessageFactory<T>.Response(T message, ConnectionOptions connectionOptions, string clientID, string responseChannel, Dictionary<string, string>? tagCollection)
            => ProduceBaseMessage(message, connectionOptions,clientID, responseChannel, tagCollection);

        Interfaces.Messages.IInternalMessage<T> IMessageFactory<T>.ConvertMessage(ILogger? logger, QueueMessage msg)
            => ConvertMessage(logger, msg.Metadata, msg.Body, msg.Tags, msg.MessageID, (msg.Attributes.Timestamp==0 ? DateTime.Now: Utility.FromUnixTime(msg.Attributes.Timestamp)));

        Interfaces.Messages.IInternalMessage<T> IMessageFactory<T>.ConvertMessage(ILogger? logger, SRecievedMessage<Request> msg)
            => ConvertMessage(logger, msg.Data.Metadata, msg.Data.Body, msg.Data.Tags, msg.Data.RequestID, msg.Timestamp);

        Interfaces.Messages.IInternalMessage<T> IMessageFactory<T>.ConvertMessage(ILogger? logger, SRecievedMessage<EventReceive> msg)
            => ConvertMessage(logger, msg.Data.Metadata, msg.Data.Body, msg.Data.Tags,msg.Data.EventID,(msg.Data.Timestamp==0 ? msg.Timestamp : Utility.FromUnixTime(msg.Data.Timestamp)));
        IResultMessage<T> IMessageFactory<T>.ConvertMessage(ILogger? logger, Response msg)
        {
            try
            {
                return new ResultMessage<T>(response:ConvertData(logger, msg.Metadata, msg.Body, msg.Tags),tags:msg.Tags);
            }
            catch (Exception e)
            {
                return new ResultMessage<T>(error:e.Message);
            }
        }

        public bool CanConvertFrom(Type responseType)
            => converters.Any(con => con.CanConvert(responseType));

        public bool CanConvert(Type sourceType)
            => CanConvertFrom(sourceType);
    }
}
