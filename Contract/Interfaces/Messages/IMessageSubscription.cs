using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KubeMQ.Contract.Interfaces.Messages
{
    internal interface IMessageSubscription
    {
        Guid ID { get; }
        Task Start();
        void Stop();
    }
}
