using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static KubeMQ.Contract.SDK.Grpc.Subscribe.Types;

namespace KubeMQ.Contract.Attributes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class StoredMessage : Attribute
    {
        public enum MessageReadStyle
        {
            StartNewOnly = 1,
            StartFromFirst = 2,
            StartFromLast = 3,
            StartAtSequence = 4,
            StartAtTime = 5,
            StartAtTimeDelta = 6
        };

        private readonly MessageReadStyle _style;
        public MessageReadStyle ReadStyle => _style;

        internal EventsStoreType EventsStoreType => (EventsStoreType)(int)_style;

        public StoredMessage(MessageReadStyle style)
        {
            _style = style;
        }
    }
}
