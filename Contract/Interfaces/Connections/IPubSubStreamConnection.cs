using KubeMQ.Contract.Interfaces.Messages;

namespace KubeMQ.Contract.Interfaces.Connections
{
    /// <summary>
    /// Houses the calls to create a Publish or Subscribe Stream using the connection options provided.
    /// </summary>
    public interface IPubSubStreamConnection : IConnectionBase
    {
        /// <summary>
        /// Called to create a Writable Message Stream in a Pub/Sub style
        /// </summary>
        /// <typeparam name="T">The type of messages being sent</typeparam>
        /// <param name="channel">The name of the channel to transmit into.  If this is not specified here, a MessageChannel attribute is expected on T</param>
        /// <returns>A writable transmission stream design to write the connection send methods</returns>
        IWritableMessageStream<T> CreateStream<T>(string? channel = null);
        /// <summary>
        /// Called to create a Readable Message Stream in a Pub/Sub style
        /// </summary>
        /// <typeparam name="T">The type of messages being recieved</typeparam>
        /// <param name="errorRecieved">An action to be called when an error is recieved from the subscription</param>
        /// <param name="channel">The name of the channel to transmit into.  If this is not specified here, a MessageChannel attribute is expected on T</param>
        /// <param name="group">The name of a group to be subscribed as, this is used if there is more than one instance of a listener (multiple pods).  The messages will be round robined inside each group.</param>
        /// <param name="storageOffset">A supplied value if there is a messageReadStyle specified that requires an X number</param>
        /// <param name="messageReadStyle">A specific read style, will override the StoredMessage property, if the Channel being subscribed to has storage.</param>
        /// <param name="ignoreMessageHeader">If set to true, the library will ignore the message header which is used to tag the message type.  This will ignore all conversion attempts 
        /// <param name="cancellationToken">A cancellation token used to stop the subscription</param>
        /// and simply decode the message under the assumption that it is of type T</param>
        /// <returns>A readonly message stream for handling subscription events</returns>
        IReadonlyMessageStream<T> SubscribeToStream<T>(
            Action<Exception> errorRecieved,
            string? channel = null,
            string group = "",
            long storageOffset = 0,
            MessageReadStyle? messageReadStyle = null,
            bool ignoreMessageHeader=false,
            CancellationToken cancellationToken = new CancellationToken()
        );

    }
}
