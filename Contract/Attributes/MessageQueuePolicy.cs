namespace KubeMQ.Contract.Attributes
{
    /// <summary>
    /// Used to define the Queue policy (settings) for a class when 
    /// using the Queue capabilities of KubeMQ.  All these values can
    /// be overidden at the call to Enqueue a message.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class,AllowMultiple =false,Inherited =false)]
    public class MessageQueuePolicy : Attribute
    {
        internal int? ExpirationSeconds { get; private init; }
        internal int? MaxCount { get; private init; }
        internal string? MaxCountChannel { get; private init; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="expirationSeconds">The number of seconds a message can exist in the queue before being expired</param>
        /// <param name="maxCount">The maximum number of messages allowed in the queue</param>
        /// <param name="maxCountChannel">The channel to transfer messages to when the queue is full</param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public MessageQueuePolicy(int? expirationSeconds=null, int? maxCount = null, string? maxCountChannel = null)
        {
            if ((maxCount!=null && maxCountChannel==null)
               ||(maxCount==null&&maxCountChannel!=null))
                throw new ArgumentOutOfRangeException("You must specify both the maxCount and maxCountQeueue if you are specifying either");
            ExpirationSeconds=expirationSeconds;
            MaxCount=maxCount;
            MaxCountChannel=maxCountChannel;
        }
    }
}
