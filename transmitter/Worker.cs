using System;
using System.Collections.Generic;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using transmitter.Interfaces;
using transmitter.Tools;

namespace transmitter
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IReceiver _receiver;
        private readonly ISender _sender;
        private readonly IScheduler _scheduler;
        private readonly Database _database;

        public Worker(ILogger<Worker> logger, IReceiver receiver, Database database, ISender sender, IScheduler scheduler)
        {
            _logger = logger;
            _receiver = receiver;
            _sender = sender;
            _scheduler = scheduler;
            _database = database;
        }
        
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Creating new receiver observable.");    
                var observable = await _receiver.CreateObservable(stoppingToken);
                var doneToken = new CancellationTokenSource();
                using var disposable = observable
                    .SelectMany(async messages =>
                    {
                        await foreach (var x in messages.WithCancellation(stoppingToken))
                        {
                            using var scope = _logger.BeginScope(new Dictionary<string, string>
                            {
                                {"messageId", x.MessageId},
                                {"subject", x.Subject}
                            });
                            var date = x.Date.LocalDateTime.ToString("s");
                            if (!_database.IsNew(x.MessageId, date) || !_sender.Prepare(x))
                                continue;

                            _logger.LogInformation("New message received.");      
                            var send = _sender.Send(x);

                            var commit = _database.SaveMessage(x.MessageId, date);
                            await send;
                            if (send.IsCompletedSuccessfully)
                                commit();
                            
                        }
                        return default(object);
                    })
                    .Subscribe(
                        onNext: _ => { },
                        onError: e => _logger.LogError(e, "Unexpected error during processing."),
                        onCompleted: () => doneToken.Cancel()
                    );

                using var _ = _scheduler.ScheduleAsync((_, _) => _receiver.Start(doneToken.Token, stoppingToken));
                while (!doneToken.IsCancellationRequested)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(100), stoppingToken);
                }
            }
        }
    }
}