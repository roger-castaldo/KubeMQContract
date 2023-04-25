using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KubeMQ.Contract.Attributes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false,Inherited =false)]
    public class MessageName :Attribute
    {
        private readonly string value;
        internal string Value => value;

        public MessageName(string value)
        {
            this.value = value;
        }
    }
}
