using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KubeMQ.Contract.Interfaces
{
    public interface IStreamStats
    {
        long Errors { get; }
        long Success { get; }
    }

    public interface IWritableMessageStream<T> : IDisposable
    {
        long Length { get; }
        IStreamStats Stats { get; }
        Task<ITransmissionResult> Write(T message, CancellationToken cancellationToken = default, Dictionary<string, string>? tagCollection = null);
    }
}
