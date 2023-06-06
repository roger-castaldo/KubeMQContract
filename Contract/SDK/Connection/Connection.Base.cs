using KubeMQ.Contract.Interfaces;
using KubeMQ.Contract.Interfaces.Connections;
using KubeMQ.Contract.SDK.Grpc;
using Microsoft.Extensions.Logging;

namespace KubeMQ.Contract.SDK.Connection
{
    internal partial class Connection : IConnectionBase
    {
        public IPingResult Ping()
        {
            Log(LogLevel.Information, "Calling ping to {}", connectionOptions.Address);
            var rec = this.client.Ping(new Empty());
            Log(LogLevel.Information, "Pind result to {} Uptime seconds {}", connectionOptions.Address, rec.ServerUpTimeSeconds);
            return new KubeMQ.Contract.SDK.PingResult(rec);
        }

        public void Unsubscribe(Guid id)
        {
            Log(LogLevel.Information, "Unsubscribing from {}", id);
            lock (subscriptions)
            {
                var sub = subscriptions.FirstOrDefault(s => s.ID == id);
                if (sub!=null)
                {
                    sub.Stop();
                    subscriptions.Remove(sub);
                }
            }
        }
    }
}
