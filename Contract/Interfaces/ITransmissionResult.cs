namespace KubeMQ.Contract.Interfaces
{
    /// <summary>
    /// Houses the result from transmitting a message into the system or as part of a response
    /// </summary>
    public interface ITransmissionResult : IDisposable
    {
        /// <summary>
        /// The ID of the message that was generated during transmission
        /// </summary>
        Guid MessageID { get; }
        /// <summary>
        /// true if there is an error with the message
        /// </summary>
        bool IsError { get; }
        /// <summary>
        /// The error for the message
        /// </summary>
        string? Error { get; }
    }
}
