using KubeMQ.Contract.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Messages
{
    [MessageChannel("Greeting")]
    [MessageName("Hello")]
    [MessageVersion("2.0.0")]
    public class Hello2 : Hello
    {
        public string Salutation { get; set; } = string.Empty;
    }
}
