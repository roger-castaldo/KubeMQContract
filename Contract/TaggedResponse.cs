namespace KubeMQ.Contract
{
    /// <summary>
    /// Houses a response from an RPC call that includes header tags
    /// </summary>
    /// <typeparam name="T">The type of object housed into the Response</typeparam>
    public class TaggedResponse<T>
    {
        /// <summary>
        /// The tags desired to transmit as headers in the message
        /// </summary>
        public Dictionary<string, string>? Tags { get; init; } = null;
        /// <summary>
        /// The value of the Response from the RPC call
        /// </summary>
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public T Response { get; init; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    }
}
