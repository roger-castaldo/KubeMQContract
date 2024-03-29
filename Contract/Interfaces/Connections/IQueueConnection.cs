﻿using KubeMQ.Contract.Interfaces.Messages;

namespace KubeMQ.Contract.Interfaces.Connections
{
    /// <summary>
    /// Houses a Queue connection to a KubeMQ host and is used to Send and Recieve messages
    /// </summary>
    public interface IQueueConnection : IConnectionBase
    {
        /// <summary>
        /// Called to add a message to a Queue
        /// </summary>
        /// <typeparam name="T">The type of message being sent</typeparam>
        /// <param name="message">The message being sent</param>
        /// <param name="channel">The name of the channel to transmit into.  If this is not specified here, a MessageChannel attribute is expected on T</param>
        /// <param name="tagCollection">A set of key value pairs to me transmitted as headers attached to the message</param>
        /// <param name="expirationSeconds">The number of seconds to keep the message in the queue before expiring.  This will override the value specified in the MessageQueuePolicy attribute if specified on T</param>
        /// <param name="delaySeconds">The number of seconds to delay the message before adding it to the queue.  This will override the value specified in the MessageQueuePolicy attribute if specified on T</param>
        /// <param name="maxQueueSize">The maximum number of messages allowed in the queue before adding to the maxQueueChannel.  This will override the value specified in the MessageQueuePolicy attribute if specified on T</param>
        /// <param name="maxQueueChannel">The channel to place messages in after the queue size is exceeded.  This will override the value specified in the MessageQueuePolicy attribute if specified on T</param>
        /// <param name="cancellationToken">A cancellation token to allow for cancelling the tranmission</param>
        /// <returns>A Transmission result indicating the message id and or errors if an error occured</returns>
        Task<ITransmissionResult> EnqueueMessage<T>(
            T message, 
            string? channel = null, 
            Dictionary<string, string>? tagCollection = null, 
            int? expirationSeconds = null, 
            int? delaySeconds = null,
            int? maxQueueSize = null, 
            string? maxQueueChannel = null, 
            CancellationToken cancellationToken = new CancellationToken()
        );

        /// <summary>
        /// Called to add a batch of messages to a Queue
        /// </summary>
        /// <typeparam name="T">The type of message being sent</typeparam>
        /// <param name="messages">The messages being sent</param>
        /// <param name="channel">The name of the channel to transmit into.  If this is not specified here, a MessageChannel attribute is expected on T</param>
        /// <param name="tagCollection">A set of key value pairs to me transmitted as headers attached to the message</param>
        /// <param name="expirationSeconds">The number of seconds to keep the message in the queue before expiring.  This will override the value specified in the MessageQueuePolicy attribute if specified on T</param>
        /// <param name="delaySeconds">The number of seconds to delay the message before adding it to the queue.  This will override the value specified in the MessageQueuePolicy attribute if specified on T</param>
        /// <param name="maxQueueSize">The maximum number of messages allowed in the queue before adding to the maxQueueChannel.  This will override the value specified in the MessageQueuePolicy attribute if specified on T</param>
        /// <param name="maxQueueChannel">The channel to place messages in after the queue size is exceeded.  This will override the value specified in the MessageQueuePolicy attribute if specified on T</param>
        /// <param name="cancellationToken">A cancellation token to allow for cancelling the tranmission</param>
        /// <returns>A BatchTransmission result indicating the message id and or errors if an error occured or message ids of the messages sent</returns>
        Task<IBatchTransmissionResult> EnqueueMessages<T>(
            IEnumerable<T> messages, 
            string? channel = null, 
            Dictionary<string, string>? tagCollection = null, 
            int? expirationSeconds = null, 
            int? delaySeconds = null, 
            int? maxQueueSize = null, 
            string? maxQueueChannel = null, 
            CancellationToken cancellationToken = new CancellationToken()
        );

        /// <summary>
        /// Called to create a subscription to a Queue style Event channel
        /// </summary>
        /// <typeparam name="T">The type of message to listen for</typeparam>
        /// <param name="channel">The name of the channel to transmit into.  If this is not specified here, a MessageChannel attribute is expected on T</param>
        /// <param name="ignoreMessageHeader">If set to true, the library will ignore the message header which is used to tag the message type.  This will ignore all conversion attempts 
        /// and simply decode the message under the assumption that it is of type T</param>
        /// <param name="cancellationToken">A cancellation token used to stop the subscription</param>
        /// <returns>An instance of a IMessageQueue that will interact with the Queue on the server</returns>
        IMessageQueue<T> SubscribeToQueue<T>(
            string? channel = null,
            bool ignoreMessageHeader=false,
            CancellationToken cancellationToken = new CancellationToken()
        );

        /// <summary>
        /// Called to create a readonly message stream to a Queue style Event channel
        /// </summary>
        /// <typeparam name="T">The type of message to listen for</typeparam>
        /// <param name="channel">The name of the channel to transmit into.  If this is not specified here, a MessageChannel attribute is expected on T</param>
        /// <param name="ignoreMessageHeader">If set to true, the library will ignore the message header which is used to tag the message type.  This will ignore all conversion attempts 
        /// and simply decode the message under the assumption that it is of type T</param>
        /// <param name="cancellationToken">A cancellation token used to stop the subscription</param>
        /// <returns>An instance of a IMessageQueue that will interact with the Queue on the server</returns>
        IReadonlyMessageStream<T> SubscribeToQueueAsStream<T>(
            string? channel = null,
            bool ignoreMessageHeader=false,
            CancellationToken cancellationToken = new CancellationToken()
        );
    }
}
