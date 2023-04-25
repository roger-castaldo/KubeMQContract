using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KubeMQ.Contract.SDK.Interfaces
{
    internal interface IKubeEvent : IKubeMessage
    {
        bool Stored { get; }
    }
}
