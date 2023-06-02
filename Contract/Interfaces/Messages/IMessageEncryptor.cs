namespace KubeMQ.Contract.Interfaces.Messages
{
    /// <summary>
    /// Used to define a specific message encryptor for the type T.  
    /// This will override the global decryptor if specified for this connection 
    /// as well as the default of not encrypting the message body
    /// </summary>
    /// <typeparam name="T">The type of message that this encryptor supports</typeparam>
    public interface IMessageEncryptor<T>
    {
        /// <summary>
        /// Called to Decrypt the byte stream provided by the KubeMQ message
        /// </summary>
        /// <param name="stream">The byte stream from the message body</param>
        /// <param name="headers">The headers from the message</param>
        /// <returns>A decrypted version of the stream provided</returns>
        Stream Decrypt(Stream stream, IMessageHeader headers);
        /// <summary>
        /// Called to Encrypt the byte array of the message after being encoded
        /// </summary>
        /// <param name="data">The byte array representing the encoded message body</param>
        /// <param name="headers">The headers to be attached to the message if needed</param>
        /// <returns>An encrypted version of the provided byte array</returns>
        byte[] Encrypt(byte[] data, out Dictionary<string, string> headers);
    }
}
