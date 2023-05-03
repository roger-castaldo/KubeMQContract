using Google.Protobuf;
using KubeMQ.Contract.SDK.Grpc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KubeMQ.Contract.Interfaces
{
    /// <summary>
    /// The main component of this library.  Houses a connection to a KubeMQ host and is used to Send and Recieve messages
    /// </summary>
    public interface IConnection
    {
        /// <summary>
        /// Called to Ping the host and get status information
        /// </summary>
        /// <returns>An IPingResult that houses the status and information for the host this connection is linked to</returns>
        IPingResult Ping();

        /// <summary>
        /// Called to Send a Message in a Pub/Sub style
        /// </summary>
        /// <typeparam name="T">The type of message being sent</typeparam>
        /// <param name="message">The message being sent</param>
        /// <param name="cancellationToken">A cancellation token to allow for cancelling the tranmission</param>
        /// <param name="channel">The name of the channel to transmit into.  If this is not specified here, a MessageChannel attribute is expected on T</param>
        /// <param name="tagCollection">A set of key value pairs to me transmitted as headers attached to the message</param>
        /// <returns>A transmission result indicating the message id and or errors if an error occured</returns>
        Task<ITransmissionResult> Send<T>(T message, CancellationToken cancellationToken = new CancellationToken(), string? channel = null,Dictionary<string,string>? tagCollection=null);

        /// <summary>
        /// Called to send a Message in RPC style where a response is expected
        /// </summary>
        /// <typeparam name="T">The type of message being sent</typeparam>
        /// <typeparam name="R">The type of response expected</typeparam>
        /// <param name="message">The message being sent</param>
        /// <param name="cancellationToken">A cancellation token to allow for cancelling the tranmission</param>
        /// <param name="channel">The name of the channel to transmit into.  If this is not specified here, a MessageChannel attribute is expected on T</param>
        /// <param name="tagCollection">A set of key value pairs to me transmitted as headers attached to the message</param>
        /// <param name="timeout">The number of milliseconds to wait for a response (default 5000 unless specified on the class)</param>
        /// <param name="type">The RPC type to use, these must match on sender and reciever.  If this is not specified here, the RPCCommandType attribute is expected on T</param>
        /// <returns>A response that can contain the expected response of R or an Error</returns>
        Task<IResultMessage<R>> SendRPC<T, R>(T message, CancellationToken cancellationToken = new CancellationToken(), string? channel = null, Dictionary<string, string>? tagCollection = null, int? timeout=null, RPCType? type=null);

        /// <summary>
        /// Called to add a message to a Queue
        /// </summary>
        /// <typeparam name="T">The type of message being sent</typeparam>
        /// <param name="message">The message being sent</param>
        /// <param name="cancellationToken">A cancellation token to allow for cancelling the tranmission</param>
        /// <param name="channel">The name of the channel to transmit into.  If this is not specified here, a MessageChannel attribute is expected on T</param>
        /// <param name="tagCollection">A set of key value pairs to me transmitted as headers attached to the message</param>
        /// <param name="expirationSeconds">The number of seconds to keep the message in the queue before expiring.  This will override the value specified in the MessageQueuePolicy attribute if specified on T</param>
        /// <param name="delaySeconds">The number of seconds to delay the message before adding it to the queue.  This will override the value specified in the MessageQueuePolicy attribute if specified on T</param>
        /// <param name="maxQueueSize">The maximum number of messages allowed in the queue before adding to the maxQueueChannel.  This will override the value specified in the MessageQueuePolicy attribute if specified on T</param>
        /// <param name="maxQueueChannel">The channel to place messages in after the queue size is exceeded.  This will override the value specified in the MessageQueuePolicy attribute if specified on T</param>
        /// <returns>A Transmission result indicating the message id and or errors if an error occured</returns>
        Task<ITransmissionResult> EnqueueMessage<T>(T message, CancellationToken cancellationToken = new CancellationToken(), string? channel = null, Dictionary<string, string>? tagCollection = null, int? expirationSeconds = null,int? delaySeconds=null,int? maxQueueSize=null,string? maxQueueChannel=null);

        /// <summary>
        /// Called to add a batch of messages to a Queue
        /// </summary>
        /// <typeparam name="T">The type of message being sent</typeparam>
        /// <param name="messages">The messages being sent</param>
        /// <param name="cancellationToken">A cancellation token to allow for cancelling the tranmission</param>
        /// <param name="channel">The name of the channel to transmit into.  If this is not specified here, a MessageChannel attribute is expected on T</param>
        /// <param name="tagCollection">A set of key value pairs to me transmitted as headers attached to the message</param>
        /// <param name="expirationSeconds">The number of seconds to keep the message in the queue before expiring.  This will override the value specified in the MessageQueuePolicy attribute if specified on T</param>
        /// <param name="delaySeconds">The number of seconds to delay the message before adding it to the queue.  This will override the value specified in the MessageQueuePolicy attribute if specified on T</param>
        /// <param name="maxQueueSize">The maximum number of messages allowed in the queue before adding to the maxQueueChannel.  This will override the value specified in the MessageQueuePolicy attribute if specified on T</param>
        /// <param name="maxQueueChannel">The channel to place messages in after the queue size is exceeded.  This will override the value specified in the MessageQueuePolicy attribute if specified on T</param>
        /// <returns>A BatchTransmission result indicating the message id and or errors if an error occured or message ids of the messages sent</returns>
        Task<IBatchTransmissionResult> EnqueueMessages<T>(IEnumerable<T> messages, CancellationToken cancellationToken = new CancellationToken(), string? channel = null, Dictionary<string, string>? tagCollection = null, int? expirationSeconds = null, int? delaySeconds = null, int? maxQueueSize = null, string? maxQueueChannel = null);

        /// <summary>
        /// Called to create a subscription to a Pub/Sub style Event channel
        /// </summary>
        /// <typeparam name="T">The type of message to be listening for</typeparam>
        /// <param name="messageRecieved">The callback to be called when a message is recieved</param>
        /// <param name="errorRecieved">The callback to be called if an error is recieved</param>
        /// <param name="cancellationToken">A cancellation token used to stop the subscription</param>
        /// <param name="channel">The name of the channel to transmit into.  If this is not specified here, a MessageChannel attribute is expected on T</param>
        /// <param name="group">The name of a group to be subscribed as, this is used if there is more than one instance of a listener (multiple pods).  The messages will be round robined inside each group.</param>
        /// <param name="storageOffset">A supplied value if there is a messageReadStyle specified that requires an X number</param>
        /// <param name="messageReadStyle">A specific read style, will override the StoredMessage property, if the Channel being subscribed to has storage.</param>
        /// <returns>A unique ID for this particular subscription that can be used to Unsubscribe</returns>
        Guid Subscribe<T>(
            Action<IMessage<T>> messageRecieved, 
            Action<string> errorRecieved, 
            CancellationToken cancellationToken = new CancellationToken(), 
            string? channel = null, 
            string group = "",
            long storageOffset=0,
            MessageReadStyle? messageReadStyle = null);

        /// <summary>
        /// Called to create a subscription to a RPC style Event channel
        /// </summary>
        /// <typeparam name="T">The type of message to listen for</typeparam>
        /// <typeparam name="R">The type of message to response with</typeparam>
        /// <param name="processMessage">The callback to be called when a message is recieved and a response is needed</param>
        /// <param name="errorRecieved">The callback to be called if an error is recieved</param>
        /// <param name="cancellationToken">A cancellation token used to stop the subscription</param>
        /// <param name="channel">The name of the channel to transmit into.  If this is not specified here, a MessageChannel attribute is expected on T</param>
        /// <param name="group">The name of a group to be subscribed as, this is used if there is more than one instance of a listener (multiple pods).  The messages will be round robined inside each group.</param>
        /// <param name="commandType">A specific RPCType, this will override the one specified by the RPCommandType attribute.  WARNING: these values must match on both the sender and reciever.</param>
        /// <returns>A unique ID for this particular subscription that can be used to Unsubscribe</returns>
        Guid SubscribeRPC<T, R>(
            Func<IMessage<T>, TaggedResponse<R>> processMessage,
            Action<string> errorRecieved,
            CancellationToken cancellationToken = new CancellationToken(),
            string? channel = null,
            string group = "",
            RPCType? commandType=null
        );

        /// <summary>
        /// Called to create a subscription to a Queue style Event channel
        /// </summary>
        /// <typeparam name="T">The type of message to listen for</typeparam>
        /// <param name="cancellationToken">A cancellation token used to stop the subscription</param>
        /// <param name="channel">The name of the channel to transmit into.  If this is not specified here, a MessageChannel attribute is expected on T</param>
        /// <returns>An instance of a IMessageQueue that will interact with the Queue on the server</returns>
        IMessageQueue<T> SubscribeToQueue<T>(
            CancellationToken cancellationToken = new CancellationToken(),
            string? channel = null
        );

        /// <summary>
        /// Called to remove a subscription from the connection
        /// </summary>
        /// <param name="id">The unique id that was provided by the subscribe call</param>
        void Unsubscribe(Guid id);
    }
}
