using System;
using Azure.Messaging.EventGrid;
using Azure.Messaging.EventHubs.Producer;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Primitives;
using Azure.Messaging.EventHubs.Processor;
using Azure.Storage.Blobs;
using Azure.Messaging.EventHubs.Administration;
using Azure.Messaging.ServiceBus.Administration;
using AzurePubSub.Abstractions;
using AzurePubSub.EventGridImpl;
using AzurePubSub.EventHubsImpl;
using AzurePubSub.Management;
using AzurePubSub.Serialization;
using Microsoft.Extensions.DependencyInjection;

namespace AzurePubSub.Extensions
{
    public static class EventGridEventHubsExtensions
    {
        public static IServiceCollection AddAzureEventGridPubSub(this IServiceCollection services, EventGridPublisherClient client)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            if (client == null) throw new ArgumentNullException(nameof(client));

            services.AddSingleton(client);
            services.AddSingleton<IMessageSerializer, JsonMessageSerializer>();
            services.AddSingleton<IPublisher, EventGridPublisher>();

            return services;
        }

        public static IServiceCollection AddAzureEventHubsPubSub(this IServiceCollection services, string eventHubsConnectionString, string eventHubName, BlobContainerClient? checkpointStore = null, string consumerGroup = EventHubConsumerClient.DefaultConsumerGroupName)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            if (string.IsNullOrWhiteSpace(eventHubsConnectionString)) throw new ArgumentException(nameof(eventHubsConnectionString));

            var producer = new EventHubProducerClient(eventHubsConnectionString, eventHubName);
            services.AddSingleton(producer);
            services.AddSingleton<IMessageSerializer, JsonMessageSerializer>();
            services.AddSingleton<IPublisher, EventHubsPublisher>();

            if (checkpointStore != null)
            {
                var processor = new EventProcessorClient(checkpointStore, consumerGroup, eventHubsConnectionString, eventHubName);
                services.AddSingleton(processor);
                services.AddSingleton<ISubscriber, EventHubsSubscriber>();
            }

            // register administration client for management helpers
            var admin = new EventHubAdministrationClient(eventHubsConnectionString);
            services.AddSingleton(admin);
            services.AddSingleton<EventHubManager>();

            return services;
        }

        public static IServiceCollection AddAzurePubSubManagement(this IServiceCollection services, string serviceBusConnectionString, string? eventHubsConnectionString = null)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            if (string.IsNullOrWhiteSpace(serviceBusConnectionString)) throw new ArgumentException(nameof(serviceBusConnectionString));

            var sbAdmin = new ServiceBusAdministrationClient(serviceBusConnectionString);
            services.AddSingleton(sbAdmin);
            services.AddSingleton<ServiceBusManager>();

            if (!string.IsNullOrWhiteSpace(eventHubsConnectionString))
            {
                var ehAdmin = new EventHubAdministrationClient(eventHubsConnectionString);
                services.AddSingleton(ehAdmin);
                services.AddSingleton<EventHubManager>();
            }

            return services;
        }
    }
}