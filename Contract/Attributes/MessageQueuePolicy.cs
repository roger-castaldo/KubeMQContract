using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KubeMQ.Contract.Attributes
{
    [AttributeUsage(AttributeTargets.Class,AllowMultiple =false,Inherited =false)]
    public class MessageQueuePolicy : Attribute
    {
        private readonly int? expirationSeconds;
        internal int? ExpirationSeconds => expirationSeconds;
        private readonly int? maxCount;
        internal int? MaxCount => maxCount;
        private readonly string? maxCountChannel;
        internal string? MaxCountChannel => maxCountChannel;

        public MessageQueuePolicy(int? expirationSeconds=null, int? maxCount = null, string? maxCountChannel = null)
        {
            this.expirationSeconds=expirationSeconds;
            this.maxCount=maxCount;
            this.maxCountChannel=maxCountChannel;
            if ((maxCount!=null && maxCountChannel==null)
                ||(maxCount==null&&maxCountChannel!=null))
                throw new ArgumentOutOfRangeException("You must specify both the maxCount and maxCountQeueue if you are specifying either");
        }
    }
}
