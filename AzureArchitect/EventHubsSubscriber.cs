using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.EventHubs.Processor;
using Azure.Messaging.EventHubs;
using Azure.Storage.Blobs;
using AzurePubSub.Abstractions;
using AzurePubSub.Common;
using Microsoft.Extensions.Logging;

namespace AzurePubSub.EventHubsImpl
{
    public sealed class EventHubsSubscriber : ISubscriber
    {
        private readonly EventProcessorClient _processor;
        private readonly IMessageSerializer _serializer;
        private readonly ILogger<EventHubsSubscriber>? _logger;

        public EventHubsSubscriber(EventProcessorClient processor, IMessageSerializer serializer, ILogger<EventHubsSubscriber>? logger = null)
        {
            _processor = processor ?? throw new ArgumentNullException(nameof(processor));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _logger = logger;
        }

        public async Task StartAsync(string topicName, string subscriptionName, Func<IMessage, Task> handler)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            // topicName and subscriptionName are logical in this abstraction. EventProcessorClient is already bound to an Event Hub + consumer group.
            _processor.ProcessEventAsync += async args =>
            {
                try
                {
                    var body = args.Data.EventBody.ToString();
                    var properties = new Dictionary<string, string>();
                    foreach (var kv in args.Data.Properties)
                    {
                        properties[kv.Key] = kv.Value?.ToString() ?? string.Empty;
                    }

                    var message = new Message(body, properties);
                    await handler(message).ConfigureAwait(false);

                    // checkpoint so processed events are persisted
                    try
                    {
                        await args.UpdateCheckpointAsync(args.CancellationToken).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWarning(ex, "Failed to checkpoint event");
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error handling event");
                }
            };

            _processor.ProcessErrorAsync += args =>
            {
                _logger?.LogError(args.Exception, "EventProcessor error (Partition: {PartitionId})", args.PartitionId);
                return Task.CompletedTask;
            };

            await _processor.StartProcessingAsync().ConfigureAwait(false);
            _logger?.LogInformation("Started Event Hubs processor");
        }

        public async Task StopAsync()
        {
            try
            {
                await _processor.StopProcessingAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Error stopping Event Hubs processor");
            }
        }

        public void Dispose()
        {
            _ = StopAsync();
        }
    }
}