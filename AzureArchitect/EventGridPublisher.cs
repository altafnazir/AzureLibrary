using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Azure.Messaging.EventGrid;
using Azure;
using AzurePubSub.Abstractions;
using Microsoft.Extensions.Logging;
using Azure.Core;

namespace AzurePubSub.EventGridImpl
{
    public sealed class EventGridPublisher : IPublisher
    {
        private readonly EventGridPublisherClient _client;
        private readonly IMessageSerializer _serializer;
        private readonly ILogger<EventGridPublisher>? _logger;

        public EventGridPublisher(EventGridPublisherClient client, IMessageSerializer serializer, ILogger<EventGridPublisher>? logger = null)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _logger = logger;
        }

        public async Task PublishAsync(string topicName, object payload, IDictionary<string, string>? properties = null)
        {
            if (string.IsNullOrWhiteSpace(topicName)) throw new ArgumentException("topicName is required", nameof(topicName));

            var msg = _serializer.Serialize(payload, properties);
            var data = BinaryData.FromString(msg.Body);

            // Use topicName as subject so callers can correlate (Event Grid subject is free-form)
            var evt = new EventGridEvent(
                subject: topicName,
                eventType: "AzurePubSub.CustomEvent",
                dataVersion: "1.0",
                data: data);

            foreach (var kv in msg.Properties)
            {
                // EventGridEvent doesn't have ApplicationProperties; embed as event metadata via the "Extensions" dictionary
                evt.TryAddExtension(kv.Key, kv.Value);
            }

            await _client.SendEventAsync(evt).ConfigureAwait(false);
            _logger?.LogDebug("Published EventGrid event for subject {Topic}", topicName);
        }
    }
}