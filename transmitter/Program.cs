using System;
using System.Reactive.Concurrency;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.EventLog;
using NLog;
using NLog.Extensions.Logging;
using transmitter.Interfaces;
using transmitter.Models;
using transmitter.Security;
using transmitter.Tools;

namespace transmitter
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        private static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseWindowsService()
                .ConfigureLogging((ctx, builder) =>
                {
                    builder.AddNLog(ctx.Configuration);
                    var nlogConfig = new NLogLoggingConfiguration(ctx.Configuration.GetSection("NLog"));
                    LogManager.LogFactory.Configuration = nlogConfig;
                })
                .ConfigureAppConfiguration((_, builder) =>
                {
                    builder.AddCredentialSecrets();
                })
                .ConfigureServices((_, services) =>
                {
                    services.AddHostedService<Worker>()
                        .Configure<EventLogSettings>(config =>
                        {
                            config.LogName = "Email Transmitter";
                            config.SourceName = "Email Transmitter";
                        });
                    services.AddSqlite();
                    services.AddSingleton<IReceiver, Receiver>();
                    services.AddSingleton<ISender, Sender>();
                    services.AddSingleton<IScheduler>(Scheduler.Default);
                    
                    services.AddOptions<Smtp>();
                    services.AddOptions<Imap>();
                    services.AddOptions<Credentials>();
                });

        private static void AddOptions<T>(this IServiceCollection services) where T : class
            => OptionsServiceCollectionExtensions.AddOptions<T>(services).BindConfiguration(typeof(T).Name);
    }
}