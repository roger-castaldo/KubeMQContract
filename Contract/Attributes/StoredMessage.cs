﻿namespace KubeMQ.Contract.Attributes
{
    /// <summary>
    /// Use this attribute to specify Event Storage 
    /// as well as the reading style from the Event Storage.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class StoredMessage : Attribute
    {
        private readonly MessageReadStyle _style;
        internal MessageReadStyle Style => _style;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="style">The desired Read Style to use when using a listener</param>
        public StoredMessage(MessageReadStyle style)
        {
            _style = style;
        }
    }
}
