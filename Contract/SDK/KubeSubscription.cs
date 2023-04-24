using KubeMQ.Contract.Attributes;
using KubeMQ.Contract.SDK.Grpc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace KubeMQ.Contract.SDK
{
    internal class KubeSubscription
    {
        public string RequestID => Guid.NewGuid().ToString();
        private readonly string clientID;
        public string ClientID => clientID;
        private string channel;
        public string Channel=> channel;
        private readonly string group;
        public string Group => group;


        public KubeSubscription(Type messageType, ConnectionOptions connectionOptions, string? channel = null,string group = "")
        {
            this.clientID=connectionOptions.ClientId;
            this.channel = channel??messageType.GetCustomAttributes<MessageChannel>().Select(mc => mc.Name).FirstOrDefault(String.Empty);
            this.group=group;
            if (string.IsNullOrEmpty(this.channel))
                throw new ArgumentNullException(nameof(channel), "message must have a channel value");
            if (group==null)
                throw new ArgumentNullException(nameof(group));
        }
    }
}
