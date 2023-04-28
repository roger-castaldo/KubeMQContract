﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KubeMQ.Contract.Interfaces
{
    public interface IMessage<T> : ITransmittedMessage
    {
        T Data { get; }
    }
}
