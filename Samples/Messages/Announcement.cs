using KubeMQ.Contract.Attributes;

namespace Messages
{
    [MessageChannel("Announcements")]
    public class Announcement
    {
        string Content { get; set; } = string.Empty;
    }
}
