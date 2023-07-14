namespace Messages.Conversion
{
    public class Hello1To2 : KubeMQ.Contract.Interfaces.Conversion.IMessageConverter<Hello, Hello2>
    {
        public Hello2 Convert(Hello source)
        {
            return new Hello2()
            {
                FirstName = source.FirstName,
                LastName = source.LastName,
                Salutation = "They"
            };
        }
    }
}