namespace KubeMQ.Contract
{
    /// <summary>
    /// Thrown when an error occurs attempting to connect to the KubeMQ server.  
    /// Specifically this will be thrown when the Ping that is executed on each initial connection fails.
    /// </summary>
    public class UnableToConnect : Exception
    {
        internal UnableToConnect()
            : base("Unable to establish connection to the KubeMQ host") { }
    }

    /// <summary>
    /// Thrown when a RPC Query request has been defined but the response message 
    /// is not the expected response type and not convertable from it.
    /// </summary>
    public class InvalidQueryResponseTypeSpecified : Exception
    {
        internal InvalidQueryResponseTypeSpecified(Type type, Type forcedRType)
            : base($"Unable to define RPC Query as the request type {type.Name} is locked to a response type of {forcedRType.Name}")
        {}
    }
}
