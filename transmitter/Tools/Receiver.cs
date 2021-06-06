using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using transmitter.Interfaces;
using transmitter.Models;

namespace transmitter.Tools
{
    public class Receiver : IReceiver, IAsyncDisposable
    {
        private readonly ILogger<Receiver> _logger;
        private readonly Imap _imap;
        private readonly Credentials _credentials;
        private readonly IScheduler _scheduler;

        private IConnectableObservable<IAsyncEnumerable<MimeMessage>> _observable;
        private readonly List<IDisposable> _disposable = new();
        private readonly IObservable<IAsyncEnumerable<MimeMessage>> _periodic;
        private IImapClient _client;

        public Receiver(ILogger<Receiver> logger, IOptions<Imap> imap, IOptions<Credentials> credentials, IScheduler scheduler)
        {
            _logger = logger;
            _imap = imap.Value;
            _credentials = credentials.Value;
            _scheduler = scheduler;
            _periodic = PeriodicCheck();
        }


        public async Task<IObservable<IAsyncEnumerable<MimeMessage>>> CreateObservable(CancellationToken token)
        {
            _client = await Connect(token);
            var inbox = _client.Inbox;
            await inbox.OpenAsync(FolderAccess.ReadOnly, token);
            _observable = 
                Observable.FromEventPattern<EventHandler<EventArgs>, EventArgs>(
                     handler => inbox.CountChanged += handler,
                     handler => inbox.CountChanged -= handler, 
                     _scheduler)
                .Select(_ =>
                {
                    _logger.LogInformation("Email count changed. Searching for new message ...");
                    return Read();
                })
                .Merge(_periodic, _scheduler)
                .ObserveOn(_scheduler)
                .Publish();
            
            return _observable;
        }

        public Task Start(CancellationToken doneToken, CancellationToken stopToken)
        {
            try
            {
                _observable.Connect();
                _logger.LogInformation("Start idle ...");
                return _client.IdleAsync(doneToken, stopToken);
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "Something broke connection. Retry in process ...");
            }
            
            return Task.Delay(TimeSpan.FromSeconds(10), stopToken);
        }

        private IObservable<IAsyncEnumerable<MimeMessage>> PeriodicCheck()
        {
            return Observable.Interval(TimeSpan.FromHours(1), _scheduler)
                .StartWith(_scheduler, 0L)
#pragma warning disable 1998
                .SelectMany(async _ => Read());
#pragma warning restore 1998
        }

        private async IAsyncEnumerable<MimeMessage> Read()
        {;
            using var client = await Connect();
            await client.Inbox.OpenAsync(FolderAccess.ReadOnly);
            for (var i = 0; i < client.Inbox.Count; i++)
            {
                var message = await client.Inbox.GetMessageAsync(i);
                if (message.To.Any(x => x.ToString().Contains(_credentials.ImapUsername)))
                    yield return message;
            }
        }
        private async Task<IImapClient> Connect(CancellationToken token = default)
        {
            _logger.LogDebug("Creating new imap connection.");
            var imap = new ImapClient {ServerCertificateValidationCallback = (_, _, _, _) => true};
            await imap.ConnectAsync(_imap.HostName, _imap.Port, SecureSocketOptions.StartTls, token);
            await imap.AuthenticateAsync(_credentials.ImapUsername, _credentials.Password, token);
            _logger.LogDebug("Connected.");
            await imap.Inbox.OpenAsync(FolderAccess.ReadOnly, token);
            return imap;
        }

        public ValueTask DisposeAsync()
        {
            _disposable.ForEach(x => x?.Dispose());
            return ValueTask.CompletedTask;
        }
    }
}