using KubeMQ.Contract.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KubeMQ.Contract.SDK
{
    internal class PingResult : IPingResult
    {
        private readonly KubeMQ.Contract.SDK.Grpc.PingResult result;

        public PingResult(KubeMQ.Contract.SDK.Grpc.PingResult result)
        {
            this.result=result;
        }

        public string Host => this.result.Host;

        public string Version => this.result.Version;

        public DateTime ServerStartTime => Utility.FromUnixTime(this.result.ServerStartTime);

        public TimeSpan ServerUpTime => TimeSpan.FromSeconds(this.result.ServerUpTimeSeconds);
    }
}
