using KubeMQ.Contract.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Messages
{
    [MessageChannel("Announcements")]
    public class Announcement
    {
        string Content { get; set; } = string.Empty;
    }
}
