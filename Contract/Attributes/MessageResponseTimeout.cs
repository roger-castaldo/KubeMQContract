using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KubeMQ.Contract.Attributes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false,Inherited =false)]
    public class MessageResponseTimeout : Attribute
    {
        private readonly int _value;
        public int Value => _value;

        public MessageResponseTimeout(int value)
        {
            _value=value;
        }   
    }
}
