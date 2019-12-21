using System;
using BinanceHistoryDownloader.Extensions;
using BinanceHistoryDownloader.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BinanceHistoryDownloader.Configuration
{
    public class Startup
    {
        #region Fields

        private static IConfigurationRoot _configuration;

        #endregion

        #region Methods

        public static IServiceCollection ConfigureServices()
        {
            IServiceCollection services = new ServiceCollection();

            #region Template *** Don't touch unless you know what you are doing.

            if (_configuration == null)
                throw new InvalidOperationException(
                    "Please run Startup.Configure before registering the DI container.");

            services.UseAsyncConsole(_configuration);

            #endregion

            var apiSettings = new AppSettings();
            services.AddLogging(opt =>
            {
                opt.AddConsole(c =>
                {
                    c.TimestampFormat = "[HH:mm:ss] ";
                });
                opt.SetMinimumLevel(LogLevel.Debug);
            });
            services.Configure<BinanceKeys>(_configuration.GetSection(nameof(BinanceKeys)));
            _configuration.GetSection("ApiSettings").Bind(apiSettings);
            services.AddSingleton<IBinanceCsvWriter, BinanceCsvWriter>();

            return services;
        }

        public static void Configure()
        {
            var builder = new ConfigurationBuilder()
                .AddJsonFile
                (
                    "appsettings.json",
                    false,
                    true
                ).AddUserSecrets<BinanceKeys>();

            _configuration = builder.Build();
        }

        #endregion
    }
}