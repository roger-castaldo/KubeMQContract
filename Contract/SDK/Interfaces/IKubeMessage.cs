using Google.Protobuf.Collections;

namespace KubeMQ.Contract.SDK.Interfaces
{
    internal interface IKubeMessage
    {
        string ID { get; }
        string MetaData { get; }
        string Channel { get;}
        string ClientID { get; }
        byte[] Body { get; }
        MapField<string,string> Tags { get; }
    }
}
