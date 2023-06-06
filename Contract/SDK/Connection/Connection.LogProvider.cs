using KubeMQ.Contract.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KubeMQ.Contract.SDK.Connection
{
    internal partial class Connection : ILogProvider
    {
        #region ILogProvider
        private void Log(LogLevel level, string message, params object[]? args)
        {
#pragma warning disable CA2254 // Template should be a static expression
            connectionOptions.Logger?.Log(level, message, args);
#pragma warning restore CA2254 // Template should be a static expression
        }

        void ILogProvider.LogInformation(string message, params object[]? args)
        {
            Log(LogLevel.Information, message, args);
        }

        void ILogProvider.LogTrace(string message, params object[]? args)
        {
            Log(LogLevel.Trace, message, args);
        }

        void ILogProvider.LogWarning(string message, params object[]? args)
        {
            Log(LogLevel.Warning, message, args);
        }

        void ILogProvider.LogDebug(string message, params object[]? args)
        {
            Log(LogLevel.Debug, message, args);
        }

        void ILogProvider.LogError(string message, params object[]? args)
        {
            Log(LogLevel.Error, message, args);
        }

        void ILogProvider.LogCritical(string message, params object[]? args)
        {
            Log(LogLevel.Information, message, args);
        }
        #endregion
    }
}
