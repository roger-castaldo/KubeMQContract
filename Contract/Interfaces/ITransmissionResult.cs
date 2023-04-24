using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KubeMQ.Contract.Interfaces
{
    public interface ITransmissionResult
    {
        Guid? MessageID { get; }
        bool IsError { get; }
        string? Error { get; }
    }
}
