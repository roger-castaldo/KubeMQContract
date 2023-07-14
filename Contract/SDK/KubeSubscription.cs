using KubeMQ.Contract.Attributes;
using System.Reflection;

namespace KubeMQ.Contract.SDK
{
    internal class KubeSubscription<T>
    {
        public string ClientID { get; private init; }
        public string Channel { get; private init; }
        public string Group { get;private init; }


        public KubeSubscription(string connectionClientID,Guid id, string? channel = null,string group = "")
        {
            ClientID=$"{connectionClientID}[SUB:{id}]";
            Channel = channel??typeof(T).GetCustomAttributes<MessageChannel>().Select(mc => mc.Name).FirstOrDefault(String.Empty);
            Group=group;
            if (string.IsNullOrEmpty(Channel))
                throw new ArgumentNullException(nameof(channel), "message must have a channel value");
            if (group==null)
                throw new ArgumentNullException(nameof(group));
        }
    }
}
