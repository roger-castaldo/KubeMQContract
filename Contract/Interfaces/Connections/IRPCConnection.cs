using KubeMQ.Contract.Interfaces.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KubeMQ.Contract.Interfaces.Connections
{
    /// <summary>
    /// Houses a RPC connection to a KubeMQ host and is used to Send and Recieve messages
    /// </summary>
    public interface IRPCConnection : IConnectionBase
    {
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
        Task<IResultMessage<R>> SendRPC<T, R>(T message, CancellationToken cancellationToken = new CancellationToken(), string? channel = null, Dictionary<string, string>? tagCollection = null, int? timeout = null, RPCType? type = null);
        
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
            Action<Exception> errorRecieved,
            CancellationToken cancellationToken = new CancellationToken(),
            string? channel = null,
            string group = "",
            RPCType? commandType = null
        );
    }
}
