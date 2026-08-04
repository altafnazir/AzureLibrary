using System;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus.Administration;

namespace AzureArchitect.Management
{
    public sealed class ServiceBusManager
    {
        private readonly ServiceBusAdministrationClient _admin;

        public ServiceBusManager(ServiceBusAdministrationClient admin)
        {
            _admin = admin ?? throw new ArgumentNullException(nameof(admin));
        }

        public async Task CreateTopicIfNotExistsAsync(string topicName)
        {
            if (string.IsNullOrWhiteSpace(topicName)) throw new ArgumentException(nameof(topicName));
            if (!await _admin.TopicExistsAsync(topicName).ConfigureAwait(false))
            {
                await _admin.CreateTopicAsync(topicName).ConfigureAwait(false);
            }
        }

        public async Task CreateSubscriptionIfNotExistsAsync(string topicName, string subscriptionName)
        {
            if (string.IsNullOrWhiteSpace(topicName)) throw new ArgumentException(nameof(topicName));
            if (string.IsNullOrWhiteSpace(subscriptionName)) throw new ArgumentException(nameof(subscriptionName));

            if (!await _admin.SubscriptionExistsAsync(topicName, subscriptionName).ConfigureAwait(false))
            {
                await _admin.CreateSubscriptionAsync(topicName, subscriptionName).ConfigureAwait(false);
            }
        }
    }
}