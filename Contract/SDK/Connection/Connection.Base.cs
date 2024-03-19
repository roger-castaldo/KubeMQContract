using KubeMQ.Contract.Interfaces;
using KubeMQ.Contract.Interfaces.Connections;
using Microsoft.Extensions.Logging;

namespace KubeMQ.Contract.SDK.Connection
{
    internal partial class Connection : IConnectionBase
    {
        public IPingResult? Ping()
            =>Ping(this.client);

        private IPingResult? Ping(KubeClient grpcClient)
        {
            Log(LogLevel.Debug, "Calling ping to {Address}", connectionOptions.Address);
            var rec = grpcClient.Ping();
            if (rec==null)
                return null;
            Log(LogLevel.Information, "Ping result to {Address} Uptime seconds {UpTimeSeconds}", connectionOptions.Address, rec.ServerUpTimeSeconds);
            return new KubeMQ.Contract.SDK.PingResult(rec);
        }

        public void Unsubscribe(Guid id)
        {
            Log(LogLevel.Information, "Unsubscribing from {SubscriptionID}", id);
            dataLock.Wait();
            var sub = subscriptions.FirstOrDefault(s => s.ID == id);
            if (sub!=null)
            {
                try
                {
                    sub.Stop();
                }
                catch (Exception) { }
                subscriptions.Remove(sub);
            }
            dataLock.Release();
        }
    }
}
