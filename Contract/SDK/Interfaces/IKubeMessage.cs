using Google.Protobuf.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
