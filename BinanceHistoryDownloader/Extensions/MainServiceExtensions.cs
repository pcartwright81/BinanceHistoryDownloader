using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BinanceHistoryDownloader.Extensions
{
    public static class MainServiceExtensions
    {
        #region Methods

        public static void UseAsyncConsole(this IServiceCollection services, IConfigurationRoot configuration)
        {
            services.AddSingleton(configuration);
            services.AddTransient<Application>();
        }

        #endregion
    }
}