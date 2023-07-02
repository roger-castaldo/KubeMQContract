using KubeMQ.Contract.Attributes;

namespace Messages
{
    [MessageChannel("Greeting")]
    public class Hello
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
    }
}