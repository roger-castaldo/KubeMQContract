using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KubeMQ.Contract.Attributes
{
    /// <summary>
    /// Use this attribute to specify which type of RPC call is being made with this message.
    /// The value can be overridden at the RPC call, but there must be an RPCType specified.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false,Inherited =false)]
    public class RPCCommandType : Attribute
    {
        private readonly RPCType _type;
        internal RPCType Type => _type;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type">The type of RPC call that this class represents</param>
        public RPCCommandType(RPCType type)
        {
            _type=type;
        }
    }
}
