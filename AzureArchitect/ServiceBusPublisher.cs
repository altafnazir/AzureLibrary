using Azure.Messaging.ServiceBus;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AzureArchitect.Abstractions;
using Microsoft.Extensions.Logging;

namespace AzureArchitect.ServiceBusImpl
{
    public sealed class ServiceBusPublisher : IPublisher, IAsyncDisposable
    {
        private readonly ServiceBusClient _client;
        private readonly IMessageSerializer _serializer;
        private readonly ILogger<ServiceBusPublisher>? _logger;

        public ServiceBusPublisher(ServiceBusClient client, IMessageSerializer serializer, ILogger<ServiceBusPublisher>? logger = null)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _logger = logger;
        }

        public async Task PublishAsync(string topicName, object payload, IDictionary<string, string>? properties = null)
        {
            if (string.IsNullOrWhiteSpace(topicName)) throw new ArgumentException("topicName is required", nameof(topicName));
            var msg = _serializer.Serialize(payload, properties);

            var sender = _client.CreateSender(topicName);
            try
            {
                var sbMessage = new ServiceBusMessage(msg.Body);
                foreach (var kv in msg.Properties)
                    sbMessage.ApplicationProperties[kv.Key] = kv.Value;

                await sender.SendMessageAsync(sbMessage).ConfigureAwait(false);
                _logger?.LogDebug("Published message to topic {Topic}", topicName);
            }
            finally
            {
                await sender.DisposeAsync().ConfigureAwait(false);
            }
        }

        public async ValueTask DisposeAsync()
        {
            await _client.DisposeAsync().ConfigureAwait(false);
        }
    }
}