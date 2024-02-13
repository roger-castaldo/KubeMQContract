namespace KubeMQ.Contract.Attributes
{
    /// <summary>
    /// Used to flag a message as one that can have an HTTP source from within KubeMQ
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class UsesHttpSource : Attribute
    {
        internal string MessageTypeHeader { get; private init; }

        /// <summary>
        /// Default Constructor
        /// </summary>
        /// <param name="messageTypeHeader">The http header that contains the message type string</param>
        public UsesHttpSource(string messageTypeHeader) => MessageTypeHeader = messageTypeHeader;
    }
}
