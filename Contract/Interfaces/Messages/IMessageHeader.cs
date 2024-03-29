﻿namespace KubeMQ.Contract.Interfaces.Messages
{
    /// <summary>
    /// Houses the headers that were supplied by the respective KubeMQ message (tags)
    /// </summary>
    public interface IMessageHeader : IDisposable
    {
        /// <summary>
        /// Called to get a value for the given key if it exists
        /// </summary>
        /// <param name="tagKey">The key name</param>
        /// <returns>null if the key is not found, otherwise it returns the key value</returns>
        string? this[string tagKey] { get; }
        /// <summary>
        /// The keys available in the header
        /// </summary>
        IEnumerable<string> Keys { get; }
    }
}
