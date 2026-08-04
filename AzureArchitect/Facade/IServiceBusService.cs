using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using AzureServices.Entity;
using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace AzureArchitect.Facade
{
    public interface IServiceBusService:IAsyncDisposable
    {
        // Entity Administration Operations
        Task CreateQueueAsync(string queueName, CancellationToken cancellationToken = default);
        Task CreateTopicAsync(string topicName, CancellationToken cancellationToken = default);
        Task CreateSubscriptionAsync(string topicName, string subscriptionName, string filterExpression, CancellationToken cancellationToken = default);

        // Messaging Operations (Queues & Topics)
        Task SendMessageAsync(string queueOrTopicName, string messageBody, Dictionary<string, object>? properties = null, CancellationToken cancellationToken = default);
        Task SendMessageBatchAsync<T>(string queueOrTopicName, IEnumerable<BatchMessage<T>> messageBatch, CancellationToken cancellationToken = default);

        // Receive Operations
        Task<T?> ReceiveSingleMessageFromQueue<T>(string queueName, TimeSpan? maxWaitTime = null, JsonSerializerOptions? jsonOptions = null, CancellationToken cancellationToken = default) where T : class;
        Task<List<T>> ReceiveMessagesFromQueue<T>(string queueName, int maxMessages = 10, TimeSpan? maxWaitTime = null, JsonSerializerOptions? jsonOptions = null, CancellationToken cancellationToken = default) where T : class;
        Task<T?> ReceiveSingleMessageFromSubscription<T>(string topicName, string subscriptionName, TimeSpan? maxWaitTime = null, JsonSerializerOptions? jsonOptions = null, CancellationToken cancellationToken = default)  where T : class;
        Task<List<T>> ReceiveMessagesFromSubscription<T>(string topicName, string subscriptionName, int maxMessages = 10, TimeSpan? maxWaitTime = null, JsonSerializerOptions? jsonOptions = null, CancellationToken cancellationToken = default) where T : class;

        // Push-based Processing
        Task StartProcessorAsync(string topicName, string subscriptionName, Func<ProcessMessageEventArgs, Task> messageHandler, Func<ProcessErrorEventArgs, Task> errorHandler);
        Task StopProcessorAsync(string topicName, string subscriptionName);
        Task StartQueueProcessorAsync(string queueName, Func<ProcessMessageEventArgs, Task> messageHandler, Func<ProcessErrorEventArgs, Task> errorHandler);
        Task StopQueueProcessorAsync(string queueName);
    }
}
