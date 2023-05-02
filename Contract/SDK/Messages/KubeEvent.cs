using Grpc.Core;
using KubeMQ.Contract.Attributes;
using KubeMQ.Contract.SDK.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace KubeMQ.Contract.SDK.Messages
{
    internal class KubeEvent : KubeMessage,IKubeEvent
    {
        public bool Stored { get; init; }

        public KubeEvent(IKubeMessage baseMessage)
        {
            ID=baseMessage.ID;
            MetaData=baseMessage.MetaData;
            Channel=baseMessage.Channel;
            ClientID=baseMessage.ClientID;
            Body=baseMessage.Body;
            Tags = baseMessage.Tags;
        }
    }
}
