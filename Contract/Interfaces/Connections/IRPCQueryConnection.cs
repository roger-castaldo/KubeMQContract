using KubeMQ.Contract.Interfaces.Messages;

namespace KubeMQ.Contract.Interfaces.Connections
{
    /// <summary>
    /// Houses a RPC connection to a KubeMQ host and is used to Send and Recieve messages
    /// </summary>
    public interface IRPCQueryConnection : IConnectionBase
    {
        /// <summary>
        /// Called to send a Message in RPC style where a response is expected
        /// </summary>
        /// <typeparam name="T">The type of message being sent</typeparam>
        /// <typeparam name="R">The type of response expected</typeparam>
        /// <param name="message">The message being sent</param>
        /// <param name="channel">The name of the channel to transmit into.  If this is not specified here, a MessageChannel attribute is expected on T</param>
        /// <param name="tagCollection">A set of key value pairs to me transmitted as headers attached to the message</param>
        /// <param name="timeout">The number of milliseconds to wait for a response (default 5000 unless specified on the class)</param>
        /// <param name="cancellationToken">A cancellation token to allow for cancelling the tranmission</param>
        /// <returns>A response that can contain the expected response of R or an Error</returns>
        Task<IResultMessage<R>> SendRPCQuery<T, R>(T message, string? channel = null, Dictionary<string, string>? tagCollection = null, int? timeout = null, CancellationToken cancellationToken = new CancellationToken());

        /// <summary>
        /// Called to create a subscription to a RPC style Event channel where messages are processed
        /// one at a time
        /// </summary>
        /// <typeparam name="T">The type of message to listen for</typeparam>
        /// <typeparam name="R">The type of message to response with</typeparam>
        /// <param name="processMessage">The callback to be called when a message is recieved and a response is needed</param>
        /// <param name="errorRecieved">The callback to be called if an error is recieved</param>
        /// <param name="channel">The name of the channel to transmit into.  If this is not specified here, a MessageChannel attribute is expected on T</param>
        /// <param name="group">The name of a group to be subscribed as, this is used if there is more than one instance of a listener (multiple pods).  The messages will be round robined inside each group.</param>
        /// <param name="cancellationToken">A cancellation token used to stop the subscription</param>
        /// <returns>A unique ID for this particular subscription that can be used to Unsubscribe</returns>
        Guid SubscribeRPCQuery<T, R>(
            Func<IMessage<T>, TaggedResponse<R>> processMessage,
            Action<Exception> errorRecieved,
            string? channel = null,
            string group = "",
            CancellationToken cancellationToken = new CancellationToken()
        );

        /// <summary>
        /// Called to create a subscription to a RPC style Event channel where messages are processed
        /// in individual non awaited tasks
        /// </summary>
        /// <typeparam name="T">The type of message to listen for</typeparam>
        /// <typeparam name="R">The type of message to response with</typeparam>
        /// <param name="processMessage">The callback to be called when a message is recieved and a response is needed</param>
        /// <param name="errorRecieved">The callback to be called if an error is recieved</param>
        /// <param name="channel">The name of the channel to transmit into.  If this is not specified here, a MessageChannel attribute is expected on T</param>
        /// <param name="group">The name of a group to be subscribed as, this is used if there is more than one instance of a listener (multiple pods).  The messages will be round robined inside each group.</param>
        /// <param name="cancellationToken">A cancellation token used to stop the subscription</param>
        /// <returns>A unique ID for this particular subscription that can be used to Unsubscribe</returns>
        Guid SubscribeRPCQueryAsync<T, R>(
            Func<IMessage<T>, TaggedResponse<R>> processMessage,
            Action<Exception> errorRecieved,
            string? channel = null,
            string group = "",
            CancellationToken cancellationToken = new CancellationToken()
        );
    }
}
