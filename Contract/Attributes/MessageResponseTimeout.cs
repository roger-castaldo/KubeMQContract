namespace KubeMQ.Contract.Attributes
{
    /// <summary>
    /// Use this attribute to specify the timeout (in milliseconds) for a response 
    /// from an RPC call for the specific class that this is attached to.  This can 
    /// be overridden by supplying a timeout value when making an RPC call.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false,Inherited =false)]
    public class MessageResponseTimeout : Attribute
    {
        internal int Value { get; private init; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value">The number of milliseconds for an RPC call response to return</param>
        public MessageResponseTimeout(int value) => Value = value;
    }
}
