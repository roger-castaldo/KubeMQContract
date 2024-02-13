namespace KubeMQ.Contract.Attributes
{
    /// <summary>
    /// Used to tag the version number of a specific message class.
    /// By default all messages are tagged as version 0.0.0.0.
    /// By using this tag, combined with the MessageName you can create multiple
    /// versions of the same message and if you create converters for those versions
    /// it allows you to not necessarily update code for call handling immediately.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class,AllowMultiple =false,Inherited =false)]
    public class MessageVersion : Attribute
    {
        internal Version Version { get; private init; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="version">The version number to tag this message class during transmission</param>
        public MessageVersion(string version) => Version=new Version(version);
    }
}
