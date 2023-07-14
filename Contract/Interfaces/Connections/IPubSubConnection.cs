using KubeMQ.Contract.Interfaces.Messages;

namespace KubeMQ.Contract.Interfaces.Connections
{
    /// <summary>
    /// Houses a Pub/Sub connection to a KubeMQ host and is used to Send and Recieve messages
    /// </summary>
    public interface IPubSubConnection : IConnectionBase
    {
        /// <summary>
        /// Called to Send a Message in a Pub/Sub style
        /// </summary>
        /// <typeparam name="T">The type of message being sent</typeparam>
        /// <param name="message">The message being sent</param>
        /// <param name="channel">The name of the channel to transmit into.  If this is not specified here, a MessageChannel attribute is expected on T</param>
        /// <param name="tagCollection">A set of key value pairs to me transmitted as headers attached to the message</param>
        /// <param name="cancellationToken">A cancellation token to allow for cancelling the tranmission</param>
        /// <returns>A transmission result indicating the message id and or errors if an error occured</returns>
        Task<ITransmissionResult> Send<T>(T message, string? channel = null, Dictionary<string, string>? tagCollection = null, CancellationToken cancellationToken = new CancellationToken());

        /// <summary>
        /// Called to create a subscription to a Pub/Sub style Event channel
        /// </summary>
        /// <typeparam name="T">The type of message to be listening for</typeparam>
        /// <param name="messageRecieved">The callback to be called when a message is recieved</param>
        /// <param name="errorRecieved">The callback to be called if an error is recieved</param>
        /// <param name="channel">The name of the channel to transmit into.  If this is not specified here, a MessageChannel attribute is expected on T</param>
        /// <param name="group">The name of a group to be subscribed as, this is used if there is more than one instance of a listener (multiple pods).  The messages will be round robined inside each group.</param>
        /// <param name="storageOffset">A supplied value if there is a messageReadStyle specified that requires an X number</param>
        /// <param name="messageReadStyle">A specific read style, will override the StoredMessage property, if the Channel being subscribed to has storage.</param>
        /// <param name="cancellationToken">A cancellation token used to stop the subscription</param>
        /// <returns>A unique ID for this particular subscription that can be used to Unsubscribe</returns>
        Guid Subscribe<T>(
            Func<IMessage<T>,Task> messageRecieved,
            Action<Exception> errorRecieved,
            string? channel = null,
            string group = "",
            long storageOffset = 0,
            MessageReadStyle? messageReadStyle = null,
            CancellationToken cancellationToken = new CancellationToken());
    }
}
