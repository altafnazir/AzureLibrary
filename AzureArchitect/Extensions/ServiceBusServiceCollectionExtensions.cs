using AzureArchitect.Config;
using AzureArchitect.Facade;
using AzureArchitect.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Azure.Identity;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Microsoft.Extensions.Azure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureArchitect.Extensions
{
    public static class ServiceBusServiceCollectionExtensions
    {
        public static IServiceCollection AddServiceBusLibrary(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // Bind options
            services.Configure<ServiceBusOptions>(configuration.GetSection(ServiceBusOptions.SectionName));

            // Register Azure Clients via official Azure SDK DI helper
            services.AddAzureClients(clientBuilder =>
            {
                var options = configuration
                    .GetSection(ServiceBusOptions.SectionName)
                    .Get<ServiceBusOptions>();

                if (options == null)
                {
                    throw new InvalidOperationException($"Configuration section '{ServiceBusOptions.SectionName}' is missing.");
                }

                // 1. Managed Identity Authentication
                if (!string.IsNullOrWhiteSpace(options.FullyQualifiedNamespace))
                {
                    clientBuilder.AddServiceBusClientWithNamespace(options.FullyQualifiedNamespace)
                                 .WithCredential(new DefaultAzureCredential());

                    clientBuilder.AddServiceBusAdministrationClientWithNamespace(options.FullyQualifiedNamespace)
                                 .WithCredential(new DefaultAzureCredential());
                }
                // 2. Connection String Authentication
                else if (!string.IsNullOrWhiteSpace(options.ConnectionString))
                {
                    clientBuilder.AddServiceBusClient(options.ConnectionString);
                    clientBuilder.AddServiceBusAdministrationClient(options.ConnectionString);
                }
                else
                {
                    throw new InvalidOperationException(
                        "Service Bus configuration requires either 'FullyQualifiedNamespace' or 'ConnectionString'.");
                }
            });

            // Register wrapper service
            services.AddSingleton<ServiceBusService>();

            services.AddSingleton<IMessagingService>(
                sp => sp.GetRequiredService<ServiceBusService>());

            services.AddSingleton<IServiceBusService>(
                sp => sp.GetRequiredService<ServiceBusService>());

            return services;
        }
    }
}
