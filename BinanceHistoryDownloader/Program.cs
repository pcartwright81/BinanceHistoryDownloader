using System.Threading.Tasks;
using BinanceHistoryDownloader.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BinanceHistoryDownloader
{
    internal class Program
    {
        private static IConfigurationRoot _configuration;
        private static ServiceProvider _serviceProvider;

        public static async Task Main(string[] args)
        {
            Configure();
            RegisterServices();
            var scope = _serviceProvider.CreateScope();
            await scope.ServiceProvider.GetRequiredService<ConsoleApplication>().Run(args);
        }

        private static void Configure()
        {
            var builder = new ConfigurationBuilder()
                .AddUserSecrets<BinanceKeys>();
            _configuration = builder.Build();
        }

        private static void RegisterServices()
        {
            var services = new ServiceCollection();
            services.AddLogging(opt =>
            {
                opt.AddConsole(c => { c.TimestampFormat = "[HH:mm:ss] "; });
                opt.SetMinimumLevel(LogLevel.Debug);
            });
            services.Configure<BinanceKeys>(_configuration.GetSection(nameof(BinanceKeys)));
            services.AddSingleton<IBinanceCsvWriter, BinanceCsvWriter>();
            services.AddSingleton<ConsoleApplication>();
            _serviceProvider = services.BuildServiceProvider(true);
        }
    }
}