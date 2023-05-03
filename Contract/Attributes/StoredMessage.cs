using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static KubeMQ.Contract.SDK.Grpc.Subscribe.Types;

namespace KubeMQ.Contract.Attributes
{
    /// <summary>
    /// Use this attribute to specify Event Storage 
    /// as well as the reading style from the Event Storage.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class StoredMessage : Attribute
    {
        private readonly MessageReadStyle _style;
        internal MessageReadStyle Style => _style;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="style">The desired Read Style to use when using a listener</param>
        public StoredMessage(MessageReadStyle style)
        {
            _style = style;
        }
    }
}
