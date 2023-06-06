using Google.Protobuf;
using Google.Protobuf.Collections;
using Grpc.Core;
using KubeMQ.Contract.Attributes;
using KubeMQ.Contract.Interfaces;
using KubeMQ.Contract.Interfaces.Conversion;
using KubeMQ.Contract.Interfaces.Messages;
using KubeMQ.Contract.Messages;
using KubeMQ.Contract.SDK.Grpc;
using KubeMQ.Contract.SDK.Interfaces;
using KubeMQ.Contract.SDK.Messages;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.RegularExpressions;
using static KubeMQ.Contract.SDK.Grpc.Request.Types;

namespace KubeMQ.Contract.Factories
{
    internal class TypeFactory<T>:IMessageFactory<T>,IConversionPath<T>
    {
        private static readonly Regex regMetaData = new(@"^(U|C)-(.+)-((\d+\.)*(\d+))$", RegexOptions.Compiled);

        private readonly IGlobalMessageEncoder? globalMessageEncoder;
        private readonly IGlobalMessageEncryptor? globalMessageEncryptor;
        private readonly IMessageEncoder<T> messageEncoder;
        private readonly IMessageEncryptor<T> messageEncryptor;
        private readonly IEnumerable<IConversionPath<T>> converters;

        private readonly string messageName = typeof(T).GetCustomAttributes<MessageName>().Select(mn => mn.Value).FirstOrDefault(typeof(T).Name);
        private readonly string messageVersion = typeof(T).GetCustomAttributes<MessageVersion>().Select(mc => mc.Version.ToString()).FirstOrDefault("0.0.0.0");
        private readonly string messageChannel = typeof(T).GetCustomAttributes<MessageChannel>().Select(mc => mc.Name).FirstOrDefault(string.Empty);
        private readonly bool stored = typeof(T).GetCustomAttributes<StoredMessage>().FirstOrDefault() != null;
        private readonly RPCType? rpcType = (typeof(T).GetCustomAttributes<RPCCommandType>().Any() ? typeof(T).GetCustomAttributes<RPCCommandType>().First().Type : null);
        private readonly int requestTimeout = typeof(T).GetCustomAttributes<MessageResponseTimeout>().Select(mrt => mrt.Value).FirstOrDefault(5000);

        public TypeFactory(IGlobalMessageEncoder? globalMessageEncoder, IGlobalMessageEncryptor? globalMessageEncryptor)
        {
            this.globalMessageEncoder = globalMessageEncoder;
            this.globalMessageEncryptor = globalMessageEncryptor;
            var types = AssemblyLoadContext.All
                .SelectMany(context => context.Assemblies)
                .SelectMany(assembly => assembly.GetTypes())
                .Where(t=>!t.IsInterface && !t.IsAbstract);
            messageEncoder = (IMessageEncoder<T>)Activator.CreateInstance(types
                .FirstOrDefault(type => type.GetInterfaces().Any(iface => iface==typeof(IMessageEncoder<T>)),typeof(JsonEncoder<T>))
                )!;
            messageEncryptor = (IMessageEncryptor<T>)Activator.CreateInstance(types
                .FirstOrDefault(type => type.GetInterfaces().Any(iface => iface==typeof(IMessageEncryptor<T>)),typeof(NonEncryptor<T>))
                )!;
            converters = TraceConverters(typeof(T), globalMessageEncoder, globalMessageEncryptor, types,Array.Empty<object>(), Array.Empty<IConversionPath<T>>());
        }

        private static IEnumerable<IConversionPath<T>> TraceConverters(Type destinationType, IGlobalMessageEncoder? globalMessageEncoder, IGlobalMessageEncryptor? globalMessageEncryptor, IEnumerable<Type> types,IEnumerable<object> curPath, IEnumerable<IConversionPath<T>> converters)
        {
            var subPaths = types.Where(t => t.GetInterfaces().Any(iface => iface.IsGenericType &&
                iface.GetGenericTypeDefinition()==typeof(IMessageConverter<,>)
                && iface.GetGenericArguments()[1]==destinationType
                && !converters.Any(conv => conv.GetType().GetGenericArguments()[0]==iface.GetGenericArguments()[0]))
            )
                .Select(t => curPath.Prepend(Activator.CreateInstance(t)!));

            var results = converters.Concat(
                subPaths.Select(path => (IConversionPath<T>)Activator.CreateInstance(typeof(ConversionPath<,>).MakeGenericType(new Type[] {
                    ExtractGenericArguements(path.First().GetType())[0],
                    typeof(T)
                }), path,types,globalMessageEncoder,globalMessageEncryptor)!)
            );

            foreach (var path in subPaths)
                results = TraceConverters(ExtractGenericArguements(path.First().GetType())[0], globalMessageEncoder, globalMessageEncryptor, types, path, results);

            return results;
        }

        private static Type[] ExtractGenericArguements(Type t)
        {
            return t.GetInterfaces().First(iface => iface.IsGenericType && iface.GetGenericTypeDefinition()==typeof(IMessageConverter<,>)).GetGenericArguments();
        }

        private static bool IsMessageTypeMatch(string metaData, Type t, out bool isCompressed)
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

        private T ConvertData(ILogProvider logProvider, string metaData, ByteString body, MapField<string, string>? tags)
        {
            if (metaData==null)
                throw new ArgumentNullException(nameof(metaData));
            IConversionPath<T>? converter = null;
            if (IsMessageTypeMatch(metaData, typeof(T), out bool compressed))
                converter = this;
            else
            {
                foreach (var conv in converters)
                {
                    if (IsMessageTypeMatch(metaData, conv.GetType().GetGenericArguments()[0], out compressed))
                    {
                        converter=conv;
                        break;
                    }
                }
            }
            if (converter==null)
                throw new InvalidCastException();
            var stream = (compressed ? (Stream)new GZipStream(new MemoryStream(body.ToByteArray()), System.IO.Compression.CompressionMode.Decompress) : (Stream)new MemoryStream(body.ToByteArray()));
            var result = converter.ConvertMessage(logProvider, stream, new MessageHeaders() { Tags=tags });
            if (result==null)
                throw new NullReferenceException(nameof(result));
            return result;
        }

        private Interfaces.Messages.IMessage<T> ConvertMessage(ILogProvider logProvider, string metaData, ByteString body, MapField<string, string>? tags,string id,DateTime timestamp)
        {
            try
            {
                return new Message<T>()
                {
                    Data=ConvertData(logProvider, metaData, body, tags),
                    Tags=tags,
                    ID=id,
                    Timestamp=timestamp
                };
            }catch (Exception e)
            {
                return new Message<T>()
                {
                    Error=e.Message,
                    Tags=tags,
                    ID=id,
                    Timestamp=timestamp
                };
            }
        }

        T? IConversionPath<T>.ConvertMessage(ILogProvider logProvider, Stream stream, IMessageHeader messageHeader)
        {
            if (globalMessageEncryptor!=null && messageEncryptor is NonEncryptor<T>)
                stream=globalMessageEncryptor.Decrypt(stream,messageHeader);
            else
                stream = messageEncryptor.Decrypt(stream, messageHeader);
            if (globalMessageEncoder!=null && messageEncoder is JsonEncoder<T>)
                return globalMessageEncoder.Decode<T>(stream);
            else 
                return messageEncoder.Decode(stream);
        }

        private IKubeMessage ProduceBaseMessage(T message, ConnectionOptions connectionOptions, string? channel, Dictionary<string, string>? tagCollection)
        {
            channel ??= messageChannel;
            if (string.IsNullOrEmpty(channel))
                throw new ArgumentNullException(nameof(Channel), "message must have a channel value");

            var body = messageEncoder.Encode(message);

            Dictionary<string, string> messageHeader;

            if (globalMessageEncryptor!=null && messageEncryptor is NonEncryptor<T>)
                body = globalMessageEncryptor.Encrypt(body, out messageHeader);
            else
                body = messageEncryptor.Encrypt(body, out messageHeader);

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
            if (messageHeader!=null)
            {
                foreach (var tag in messageHeader)
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
                ClientID=connectionOptions.ClientId,
                Channel=channel,
                Body=body,
                MetaData=metaData,
                Tags=tags
            };
        }

        IKubeEnqueue IMessageFactory<T>.Enqueue(T message, ConnectionOptions connectionOptions, string? channel, Dictionary<string, string>? tagCollection, int? delaySeconds, int? expirationSeconds, int? maxCount, string? maxCountChannel)
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
                throw new ArgumentOutOfRangeException(nameof(maxCountChannel),$"You must specify both the {nameof(maxCount)} and {nameof(maxCountChannel)} if you are specifying either");

            return new KubeEnqueue(ProduceBaseMessage(message, connectionOptions, channel, tagCollection)){
                DelaySeconds=delaySeconds,
                ExpirationSeconds=expirationSeconds,
                MaxSize=maxCount,
                MaxCountChannel = maxCountChannel 
            };
        }

        IKubeBatchEnqueue IMessageFactory<T>.Enqueue(IEnumerable<T> messages, ConnectionOptions connectionOptions, string? channel, Dictionary<string, string>? tagCollection, int? delaySeconds, int? expirationSeconds, int? maxCount, string? maxCountChannel)
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

            return new KubeBatchEnqueue(){
                Messages=messages.Select(message =>
                {
                    var msg = new KubeEnqueue(ProduceBaseMessage(message, connectionOptions, channel, tagCollection))
                    {
                        DelaySeconds=delaySeconds,
                        ExpirationSeconds=expirationSeconds,
                        MaxSize=maxCount,
                        MaxCountChannel = maxCountChannel
                    };
                    return new QueueMessage()
                    {
                        MessageID= msg.ID,
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

        IKubeEvent IMessageFactory<T>.Event(T message, ConnectionOptions connectionOptions, string? channel, Dictionary<string, string>? tagCollection)
        {
            return new KubeEvent(ProduceBaseMessage(message, connectionOptions, channel, tagCollection))
            {
                Stored=stored
            };
        }

        IKubeRequest IMessageFactory<T>.Request<R>(T message, ConnectionOptions connectionOptions, string? channel, Dictionary<string, string>? tagCollection, int? timeout, RPCType? type)
        {
            type ??= rpcType;
            if (type==null)
                throw new ArgumentNullException(nameof(type), "message must have an RPC type value");
            return new KubeRequest(ProduceBaseMessage(message, connectionOptions, channel, tagCollection))
            {
                Timeout=timeout??requestTimeout,
                CommandType=(RequestType)(int)type.Value
            };
        }

        IKubeMessage IMessageFactory<T>.Response(T message, ConnectionOptions connectionOptions, string responseChannel, Dictionary<string, string>? tagCollection)
        {
            return ProduceBaseMessage(message, connectionOptions, responseChannel, tagCollection);
        }

        Interfaces.Messages.IMessage<T> IMessageFactory<T>.ConvertMessage(ILogProvider logProvider, QueueMessage msg)
            => ConvertMessage(logProvider, msg.Metadata, msg.Body, msg.Tags, msg.MessageID, (msg.Attributes.Timestamp==0 ? DateTime.Now : Utility.FromUnixTime(msg.Attributes.Timestamp)));

        Interfaces.Messages.IMessage<T> IMessageFactory<T>.ConvertMessage(ILogProvider logProvider, Request msg)
            => ConvertMessage(logProvider, msg.Metadata, msg.Body, msg.Tags, msg.RequestID, DateTime.Now);

        Interfaces.Messages.IMessage<T> IMessageFactory<T>.ConvertMessage(ILogProvider logProvider, EventReceive msg)
            => ConvertMessage(logProvider, msg.Metadata, msg.Body, msg.Tags,msg.EventID,(msg.Timestamp==0 ? DateTime.Now : Utility.FromUnixTime(msg.Timestamp)));
        IResultMessage<T> IMessageFactory<T>.ConvertMessage(ILogProvider logProvider, Response msg)
        {
            try
            {
                return new ResultMessage<T>()
                {
                    Response=ConvertData(logProvider, msg.Metadata, msg.Body, msg.Tags),
                    Tags=msg.Tags
                };
            }
            catch (Exception e)
            {
                return new ResultMessage<T>()
                {
                    Error=e.Message
                };
            }
        }
    }
}
