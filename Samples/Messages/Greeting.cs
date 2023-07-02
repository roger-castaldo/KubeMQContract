using KubeMQ.Contract.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Messages
{
    public class Greeting
    {
        public string Message { get; set; } = string.Empty;
    }
}
