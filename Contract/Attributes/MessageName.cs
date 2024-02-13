namespace KubeMQ.Contract.Attributes
{
    /// <summary>
    /// Used to specify the name of the message type inside the system.  
    /// Default is to use the class name, however, this can be used to 
    /// override that and allow for different versions of a message to 
    /// have the same name withing the tranmission system.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false,Inherited =false)]
    public class MessageName :Attribute
    {
        internal string Value { get; private init; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value">The name to use for the class when transmitting</param>
        public MessageName(string value) => Value = value;
    }
}
