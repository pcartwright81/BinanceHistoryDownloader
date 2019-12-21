using System;
using BinanceHistoryDownloader.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BinanceHistoryDownloader
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            Startup.Configure();

            IServiceProvider serviceProvider = Startup.ConfigureServices()
                .BuildServiceProvider();

            serviceProvider.GetService<Application>().Run(args).Wait();
        }
    }
}