namespace KubeMQ.Contract.Interfaces.Messages
{
    /// <summary>
    /// Used to define a specific message encryptor for the type T.  
    /// This will override the global decryptor if specified for this connection 
    /// as well as the default of not encrypting the message body
    /// </summary>
    /// <typeparam name="T">The type of message that this encryptor supports</typeparam>
    public interface IMessageTypeEncryptor<T> : IMessageEncryptor
    {
    }
}
