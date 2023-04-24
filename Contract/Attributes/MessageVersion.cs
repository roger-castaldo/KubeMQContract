using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KubeMQ.Contract.Attributes
{
    [AttributeUsage(AttributeTargets.Class,AllowMultiple =false,Inherited =false)]
    public class MessageVersion : Attribute
    {
        private readonly Version _version;
        public Version Version => _version;

        public MessageVersion(Version version)
        {
            _version=version;
        }
    }
}
