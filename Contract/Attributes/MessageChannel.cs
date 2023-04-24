using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KubeMQ.Contract.Attributes
{
    [AttributeUsage(AttributeTargets.Class,AllowMultiple=false,Inherited=true)]
    public class MessageChannel : Attribute
    {
        private readonly string _name;
        public string Name => _name;

        public MessageChannel(string name) { _name=name; }
    }
}
