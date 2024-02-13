namespace KubeMQ.Contract.Attributes
{
    /// <summary>
    /// Used to lock an RPC Query request message to a specific response type
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class RPCQueryResponseType : Attribute
    {
        internal Type ResponseType { get; private init; }

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="responseType">The type of class that should be expected for a response</param>
        public RPCQueryResponseType(Type responseType) => ResponseType = responseType;
    }
}
