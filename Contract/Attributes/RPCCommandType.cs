using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KubeMQ.Contract.Attributes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false,Inherited =false)]
    public class RPCCommandType : Attribute
    {
        private readonly RPCType _type;
        public RPCType Type => _type;

        public RPCCommandType(RPCType type)
        {
            _type=type;
        }
    }
}
