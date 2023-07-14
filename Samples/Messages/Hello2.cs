using KubeMQ.Contract.Attributes;

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
