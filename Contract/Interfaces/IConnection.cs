using KubeMQ.Contract.SDK.Grpc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KubeMQ.Contract.Interfaces
{
    public interface IConnection
    {
        PingResult Ping();
        string PublishMessage<T>(T message, CancellationToken cancellationToken = new CancellationToken(), string? channel = null);
        Guid Subscribe<T>(Action<T> messageRecieved, Action<string> errorRecieved, CancellationToken cancellationToken = new CancellationToken(), string? channel = null, string group = "");
        void Unsubscribe(Guid id);
    }
}
