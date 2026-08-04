using System;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using AzureArchitect.Abstractions;
using AzureArchitect.Serialization;
using AzureArchitect.ServiceBusImpl;
using AzureArchitect.Management;
using Microsoft.Extensions.DependencyInjection;

namespace AzureArchitect.Extensions
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers Service Bus based publisher and subscriber and default JSON serializer.
        /// Provide a Service Bus connection string.
        /// </summary>
        public static IServiceCollection AddAzureServiceBusPubSub(this IServiceCollection services, string serviceBusConnectionString)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            if (string.IsNullOrWhiteSpace(serviceBusConnectionString)) throw new ArgumentException("Service Bus connection string is required", nameof(serviceBusConnectionString));

            services.AddSingleton(_ => new ServiceBusClient(serviceBusConnectionString));
            services.AddSingleton<IMessageSerializer, JsonMessageSerializer>();
            services.AddSingleton<IPublisher, ServiceBusPublisher>();
            services.AddSingleton<ISubscriber, ServiceBusSubscriber>();

            return services;
        }

        /// <summary>
        /// Registers management helpers for Service Bus (ServiceBusAdministrationClient + ServiceBusManager).
        /// Call this when you need to create topics/subscriptions from code.
        /// </summary>
        public static IServiceCollection AddAzureArchitectManagement(this IServiceCollection services, string serviceBusConnectionString)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            if (string.IsNullOrWhiteSpace(serviceBusConnectionString)) throw new ArgumentException("Service Bus connection string is required", nameof(serviceBusConnectionString));

            services.AddSingleton(_ => new ServiceBusAdministrationClient(serviceBusConnectionString));
            services.AddSingleton<ServiceBusManager>();

            return services;
        }
    }
}