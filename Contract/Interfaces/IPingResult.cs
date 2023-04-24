using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KubeMQ.Contract.Interfaces
{
    public interface IPingResult
    {
        string Host { get; }
        string Version { get; }
        long ServerStartTime { get; }
        long ServerUpTimeSeconds { get; }
    }
}
