using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KubeMQ.Contract.Interfaces
{
    internal interface ILogProvider
    {
        void LogInformation(string message, params object[]? args);
        void LogTrace(string message, params object[]? args);
        void LogWarning(string message, params object[]? args);
        void LogDebug(string message, params object[]? args);
        void LogError(string message, params object[]? args);
        void LogCritical(string message, params object[]? args);
    }
}
