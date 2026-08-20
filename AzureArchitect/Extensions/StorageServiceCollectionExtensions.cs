using Azure;
using Azure.Identity;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using AzureArchitect.Facade;
using AzureArchitect.Services;
using AzureServices.Entity;
using AzureServices.Enums;
using AzureServices.Facade;
using AzureServices.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using AzureArchitect.Services.BlobStorage;

namespace AzureArchitect.Extensions
{
    public static class StorageServiceCollectionExtensions
    {
        public static IServiceCollection AddStorageLibrary(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddOptions<StorageConfiguration>()
                    .Bind(configuration.GetSection("Storage"));

            services.AddSingleton<IBlobService, BlobService>();

            return services;
        }
    }
}
