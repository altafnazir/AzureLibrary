using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using AzurePubSub.Abstractions;
using Microsoft.Extensions.Logging;

namespace AzurePubSub.EventHubsImpl
{
    public sealed class EventHubsPublisher : IPublisher, IAsyncDisposable
    {
        private readonly EventHubProducerClient _producer;
        private readonly IMessageSerializer _serializer;
        private readonly ILogger<EventHubsPublisher>? _logger;

        public EventHubsPublisher(EventHubProducerClient producer, IMessageSerializer serializer, ILogger<EventHubsPublisher>? logger = null)
        {
            _producer = producer ?? throw new ArgumentNullException(nameof(producer));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _logger = logger;
        }

        public async Task PublishAsync(string topicName, object payload, IDictionary<string, string>? properties = null)
        {
            // topicName maps to the Event Hub name for clarity; if a producer is already bound to a hub, topicName can be ignored.
            var msg = _serializer.Serialize(payload, properties);
            using var batch = await _producer.CreateBatchAsync().ConfigureAwait(false);
            var eventData = new EventData(BinaryData.FromString(msg.Body));
            if (msg.Properties != null)
            {
                foreach (var kv in msg.Properties)
                    eventData.Properties[kv.Key] = kv.Value;
            }

            if (!batch.TryAdd(eventData))
                throw new InvalidOperationException("Message too large for event batch.");

            await _producer.SendAsync(batch).ConfigureAwait(false);
            _logger?.LogDebug("Published Event Hub event");
        }

        public async ValueTask DisposeAsync()
        {
            await _producer.DisposeAsync().ConfigureAwait(false);
        }
    }
}