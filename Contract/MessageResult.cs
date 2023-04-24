using KubeMQ.Contract.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KubeMQ.Contract
{
    internal class MessageResult : ITransmissionResult
    {
        public Guid? MessageID { get; init; }

        public bool IsError { get; init; }

        public string? Error { get; init; }
    }
}
