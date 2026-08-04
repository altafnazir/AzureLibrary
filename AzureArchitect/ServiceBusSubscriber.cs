using System;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using AzureArchitect.Abstractions;
using AzureArchitect.Common;
using Microsoft.Extensions.Logging;

namespace AzureArchitect.ServiceBusImpl
{
    public sealed class ServiceBusSubscriber : ISubscriber
    {
        private readonly ServiceBusClient _client;
        private readonly IMessageSerializer _serializer;
        private readonly ILogger<ServiceBusSubscriber>? _logger;

        private ServiceBusProcessor? _processor;

        public ServiceBusSubscriber(ServiceBusClient client, IMessageSerializer serializer, ILogger<ServiceBusSubscriber>? logger = null)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _logger = logger;
        }

        public async Task StartAsync(string topicName, string subscriptionName, Func<IMessage, Task> handler)
        {
            if (string.IsNullOrWhiteSpace(topicName)) throw new ArgumentException("topicName is required", nameof(topicName));
            if (string.IsNullOrWhiteSpace(subscriptionName)) throw new ArgumentException("subscriptionName is required", nameof(subscriptionName));
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            // If already started, stop previous
            if (_processor != null)
            {
                await StopAsync().ConfigureAwait(false);
            }

            _processor = _client.CreateProcessor(topicName, subscriptionName, new ServiceBusProcessorOptions
            {
                AutoCompleteMessages = false,
                MaxConcurrentCalls = 1
            });

            _processor.ProcessMessageAsync += async args =>
            {
                try
                {
                    var body = args.Message.Body.ToString();
                    var properties = new System.Collections.Generic.Dictionary<string, string>();
                    foreach (var kv in args.Message.ApplicationProperties)
                    {
                        properties[kv.Key] = kv.Value?.ToString() ?? string.Empty;
                    }

                    var message = new Message(body, properties);
                    await handler(message).ConfigureAwait(false);
                    await args.CompleteMessageAsync(args.Message).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error processing message. Abandoning.");
                    try
                    {
                        await args.AbandonMessageAsync(args.Message).ConfigureAwait(false);
                    }
                    catch { /* swallow */ }
                }
            };

            _processor.ProcessErrorAsync += args =>
            {
                _logger?.LogError(args.Exception, "ServiceBus processor error (Entity: {EntityPath})", args.EntityPath);
                return Task.CompletedTask;
            };

            await _processor.StartProcessingAsync().ConfigureAwait(false);
            _logger?.LogInformation("Started ServiceBus processor for {Topic}/{Subscription}", topicName, subscriptionName);
        }

        public async Task StopAsync()
        {
            if (_processor == null) return;

            try
            {
                await _processor.StopProcessingAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Error while stopping processor");
            }
            finally
            {
                await _processor.DisposeAsync().ConfigureAwait(false);
                _processor = null;
            }
        }

        public void Dispose()
        {
            _ = StopAsync();
        }
    }
}