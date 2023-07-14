namespace KubeMQ.Contract.Attributes
{
    /// <summary>
    /// Use this attribute to specify the Channel name used for transmitting this message class.
    /// This can be overidden by specifying the channel on the method calls, but a value must 
    /// be specified, either using the attribute or by specifying in the input.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class,AllowMultiple=false,Inherited=true)]
    public class MessageChannel : Attribute
    {
        private readonly string _name;
        internal string Name => _name;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name">The name of the Channel to be used for transmitting this message class.</param>
        public MessageChannel(string name) => _name=name;
    }
}
