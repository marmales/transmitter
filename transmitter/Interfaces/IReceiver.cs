using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MimeKit;

namespace transmitter.Interfaces
{
    public interface IReceiver
    {
        Task<IObservable<IAsyncEnumerable<MimeMessage>>> CreateObservable(CancellationToken token);
        Task Start(CancellationToken doneToken, CancellationToken stopToken);
    }
}