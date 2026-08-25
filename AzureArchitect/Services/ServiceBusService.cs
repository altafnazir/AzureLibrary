using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using AzureArchitect.Facade;
using AzureServices.Entity;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace AzureArchitect.Services
{
    public class ServiceBusService : IServiceBusService, IMessagingService, IAsyncDisposable
    {
        #region Fields

        private readonly ServiceBusClient _client;
        private readonly ServiceBusAdministrationClient _adminClient;
        private readonly ConcurrentDictionary<string, ServiceBusProcessor> _processors;
        private readonly ServiceBusProcessorOptions _serviceBusProcessorOptions;

        #endregion Fields

        #region Constructor

        /// <summary>
        /// Injected dependencies are managed by Microsoft.Extensions.Azure framework.
        /// </summary>
        public ServiceBusService(
            ServiceBusClient client,
            ServiceBusAdministrationClient adminClient,
            ServiceBusProcessorOptions serviceBusProcessorOptions)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _adminClient = adminClient ?? throw new ArgumentNullException(nameof(adminClient));
            _processors = new ConcurrentDictionary<string, ServiceBusProcessor>();
            _serviceBusProcessorOptions = serviceBusProcessorOptions;

            try
            {
                _adminClient.GetNamespacePropertiesAsync().GetAwaiter().GetResult();
            }
            catch (Exception)
            {
                throw;
            }
        }

        #endregion Constructor

        #region Entity Administration

        public async Task CreateQueueAsync(string queueName, CancellationToken cancellationToken = default)
        {
            if (!await _adminClient.QueueExistsAsync(queueName, cancellationToken))
            {
                await _adminClient.CreateQueueAsync(queueName, cancellationToken);
            }
        }

        public async Task CreateTopicAsync(string topicName, CancellationToken cancellationToken = default)
        {
            if (!await _adminClient.TopicExistsAsync(topicName, cancellationToken))
            {
                await _adminClient.CreateTopicAsync(topicName, cancellationToken);
            }
        }

        public async Task CreateSubscriptionAsync(string topicName, string subscriptionName, string filterExpression, CancellationToken cancellationToken = default)
        {
            await CreateTopicAsync(topicName, cancellationToken);

            if (!await _adminClient.SubscriptionExistsAsync(topicName, subscriptionName, cancellationToken))
            {
                await _adminClient.CreateSubscriptionAsync(topicName, subscriptionName, cancellationToken);

                await _adminClient.DeleteRuleAsync(topicName, subscriptionName, RuleProperties.DefaultRuleName);

                await _adminClient.CreateRuleAsync(topicName, subscriptionName,
                new CreateRuleOptions(
                    $"{subscriptionName}Rule",
                    new SqlRuleFilter(filterExpression)));
            }
        }

        #endregion

        #region Messaging (Send)

        /// <summary>
        /// Send a message to topic or queue
        /// </summary>
        /// <param name="queueOrTopicName"></param>
        /// <param name="messageBody"></param>
        /// <param name="properties"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task SendMessageAsync(string queueOrTopicName, string messageBody,
                                            Dictionary<string, object>? properties = null, CancellationToken cancellationToken = default)
        {
            bool queueExists = await _adminClient.QueueExistsAsync(queueOrTopicName, cancellationToken);
            bool topicExists = await _adminClient.TopicExistsAsync(queueOrTopicName, cancellationToken);

            if (!queueExists && !topicExists)
                throw new EntityNotFoundException($"Queue or topic '{queueOrTopicName}' does not exist.");

            await using ServiceBusSender sender = _client.CreateSender(queueOrTopicName);
            var serviceBusMessage = new ServiceBusMessage(messageBody);
            if (properties != null)
            {
                foreach (var property in properties)
                {
                    serviceBusMessage.ApplicationProperties.Add(property.Key, property.Value);
                }
            }

            serviceBusMessage.ContentType = "application/json";

            await sender.SendMessageAsync(serviceBusMessage, cancellationToken);
        }

        /// <summary>
        /// Send batch of messages to topic or queue
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="queueOrTopicName"></param>
        /// <param name="messageBatch"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task SendMessageBatchAsync<T>(string queueOrTopicName, IEnumerable<BatchMessage<T>> messageBatch, CancellationToken cancellationToken = default)
        {
            bool queueExists = await _adminClient.QueueExistsAsync(queueOrTopicName, cancellationToken);
            bool topicExists = await _adminClient.TopicExistsAsync(queueOrTopicName, cancellationToken);

            if (!queueExists && !topicExists)
                throw new EntityNotFoundException($"Queue or topic '{queueOrTopicName}' does not exist.");

            await using ServiceBusSender sender = _client.CreateSender(queueOrTopicName);
            ServiceBusMessageBatch batch = await sender.CreateMessageBatchAsync(cancellationToken);

            foreach (var item in messageBatch)
            {
                string body = item.Message is string s ? s : JsonSerializer.Serialize(item.Message);

                var serviceBusMessage = new ServiceBusMessage(body);
                
                if (item.ApplicationProperties != null)
                {
                    foreach (var kv in item.ApplicationProperties)
                    {
                        serviceBusMessage.ApplicationProperties[kv.Key] = kv.Value!;
                    }
                }
                serviceBusMessage.ContentType = "application/json";

                if (!batch.TryAddMessage(serviceBusMessage))
                {
                    await sender.SendMessagesAsync(batch, cancellationToken);

                    batch.Dispose();
                    batch = await sender.CreateMessageBatchAsync(cancellationToken);

                    if (!batch.TryAddMessage(serviceBusMessage))
                    {
                        throw new InvalidOperationException("Message is too large to fit in a Service Bus batch.");
                    }
                }
            }

            if (batch.Count > 0)
            {
                await sender.SendMessagesAsync(batch, cancellationToken);
            }
        }

        #endregion

        #region Messaging (Pull Receive)

        /// <summary>
        /// Retrieve single message from subscription
        /// </summary>
        /// <param name="topicName"></param>
        /// <param name="subscriptionName"></param>
        /// <returns></returns>
        public async Task<T?> ReceiveSingleMessageFromQueue<T>(
            string queueName,
            TimeSpan? maxWaitTime = null,
            JsonSerializerOptions? jsonOptions = null,
            CancellationToken cancellationToken = default)
            where T : class
        {
            if (!await _adminClient.QueueExistsAsync(queueName, cancellationToken))
            {
                throw new EntityNotFoundException($"Queue '{queueName}' does not exist.");
            }

            await using ServiceBusReceiver receiver = _client.CreateReceiver(queueName);

            ServiceBusReceivedMessage? receivedMessage = await receiver.ReceiveMessageAsync(
                maxWaitTime ?? TimeSpan.FromSeconds(5),
                cancellationToken);

            if (receivedMessage == null)
                return null;

            T? messageObj;
            try
            {
                var body = receivedMessage.Body.ToString();
                messageObj = JsonSerializer.Deserialize<T>(body, jsonOptions);
            }
            catch (JsonException)
            {
                // If deserialization fails, dead-letter or abandon.
                // Here we dead-letter with a reason so the message doesn't get retried infinitely.
                await receiver.DeadLetterMessageAsync(receivedMessage, "DeserializationFailed", cancellationToken: cancellationToken);
                return null;
            }

            if (messageObj != null)
            {
                try
                {
                    var validationContext = new ValidationContext(messageObj);
                    Validator.ValidateObject(messageObj, validationContext, validateAllProperties: true);
                    await receiver.CompleteMessageAsync(receivedMessage, cancellationToken);
                }
                catch (ValidationException ex)
                {
                    await receiver.DeadLetterMessageAsync(receivedMessage, "ValidationError", ex.Message, cancellationToken);
                    return null;
                }
            }
            else
            {
                await receiver.DeadLetterMessageAsync(receivedMessage, "DeserializedToNull", cancellationToken: cancellationToken);
            }

            return messageObj;
        }

        /// <summary>
        /// Retrieve multiple messages from queue
        /// </summary>
        /// <param name="queueName"></param>
        /// <param name="maxMessages"></param>
        /// <returns></returns>
        public async Task<List<T>> ReceiveMessagesFromQueue<T>(
            string queueName,
            int maxMessages = 10,
            TimeSpan? maxWaitTime = null,
            JsonSerializerOptions? jsonOptions = null,
            CancellationToken cancellationToken = default)
            where T : class
        {
            if (!await _adminClient.QueueExistsAsync(queueName, cancellationToken))
            {
                throw new EntityNotFoundException($"Queue '{queueName}' does not exist.");
            }

            await using ServiceBusReceiver receiver = _client.CreateReceiver(queueName);

            IReadOnlyList<ServiceBusReceivedMessage> receivedMessages = await receiver.ReceiveMessagesAsync(
                                                                    maxMessages: maxMessages,
                                                                    maxWaitTime: maxWaitTime ?? TimeSpan.FromSeconds(5),
                                                                    cancellationToken: cancellationToken);

            List<T> result = new List<T>();

            foreach (var rm in receivedMessages)
            {
                try
                {
                    var body = rm.Body.ToString();
                    var message = JsonSerializer.Deserialize<T>(body, jsonOptions);

                    if (message == null)
                    {
                        await receiver.DeadLetterMessageAsync(rm, "DeserializedToNull", cancellationToken: cancellationToken);
                        continue;
                    }

                    // Validate data annotations if present on T
                    var validationContext = new ValidationContext(message);
                    Validator.ValidateObject(message, validationContext, validateAllProperties: true);

                    result.Add(message);

                    await receiver.CompleteMessageAsync(rm, cancellationToken);
                }
                catch (ValidationException ex)
                {
                    await receiver.DeadLetterMessageAsync(
                        rm,
                        "ValidationError",
                        ex.Message,
                        cancellationToken);
                }
                catch (JsonException)
                {
                    await receiver.DeadLetterMessageAsync(rm, "DeserializationFailed", cancellationToken: cancellationToken);
                }
                catch
                {
                    // Don't complete the message. Let Service Bus retry it.
                    throw;
                }
            }

            return result;
        }

        /// <summary>
        /// Retrieve single message from subscription
        /// </summary>
        /// <param name="topicName"></param>
        /// <param name="subscriptionName"></param>
        /// <returns></returns>
        public async Task<T?> ReceiveSingleMessageFromSubscription<T>(
            string topicName,
            string subscriptionName,
            TimeSpan? maxWaitTime = null,
            JsonSerializerOptions? jsonOptions = null,
            CancellationToken cancellationToken = default)
            where T : class
        {
            if (!await _adminClient.TopicExistsAsync(topicName, cancellationToken) ||
                    !await _adminClient.SubscriptionExistsAsync(topicName, subscriptionName, cancellationToken))
            {
                throw new EntityNotFoundException($"Topic '{topicName}' or subscription '{subscriptionName}' does not exist.");
            }

            await using ServiceBusReceiver receiver = _client.CreateReceiver(topicName, subscriptionName);

            ServiceBusReceivedMessage? receivedMessage = await receiver.ReceiveMessageAsync(
                maxWaitTime ?? TimeSpan.FromSeconds(5),
                cancellationToken);

            if (receivedMessage == null)
                return null;

            T? messageObj;
            try
            {
                var body = receivedMessage.Body.ToString();
                messageObj = JsonSerializer.Deserialize<T>(body, jsonOptions);
            }
            catch (JsonException)
            {
                // If deserialization fails, dead-letter or abandon.
                // Here we dead-letter with a reason so the message doesn't get retried infinitely.
                await receiver.DeadLetterMessageAsync(receivedMessage, "DeserializationFailed", cancellationToken: cancellationToken);
                return null;
            }

            if (messageObj != null)
            {
                try
                {
                    var validationContext = new ValidationContext(messageObj);
                    Validator.ValidateObject(messageObj, validationContext, validateAllProperties: true);
                    await receiver.CompleteMessageAsync(receivedMessage, cancellationToken);
                }
                catch (ValidationException ex)
                {
                    await receiver.DeadLetterMessageAsync(receivedMessage, "ValidationError", ex.Message, cancellationToken);
                    return null;
                }
            }
            else
            {
                await receiver.DeadLetterMessageAsync(receivedMessage, "DeserializedToNull", cancellationToken: cancellationToken);
            }

            return messageObj;
        }

        /// <summary>
        /// Retrieve multiple messages from subscription
        /// </summary>
        /// <param name="topicName"></param>
        /// <param name="subscriptionName"></param>
        /// <param name="maxMessages"></param>
        /// <returns></returns>
        public async Task<List<T>> ReceiveMessagesFromSubscription<T>(
            string topicName,
            string subscriptionName,
            int maxMessages = 10,
            TimeSpan? maxWaitTime = null,
            JsonSerializerOptions? jsonOptions = null,
            CancellationToken cancellationToken = default)
            where T : class
        {
            if (!await _adminClient.TopicExistsAsync(topicName, cancellationToken) ||
                    !await _adminClient.SubscriptionExistsAsync(topicName, subscriptionName, cancellationToken))
            {
                throw new EntityNotFoundException($"Topic '{topicName}' or subscription '{subscriptionName}' does not exist.");
            }

            await using ServiceBusReceiver receiver = _client.CreateReceiver(topicName, subscriptionName);

            IReadOnlyList<ServiceBusReceivedMessage> receivedMessages = await receiver.ReceiveMessagesAsync(
                                                                    maxMessages: maxMessages,
                                                                    maxWaitTime: maxWaitTime ?? TimeSpan.FromSeconds(5),
                                                                    cancellationToken: cancellationToken);

            List<T> result = new List<T>();

            foreach (var rm in receivedMessages)
            {
                try
                {
                    var body = rm.Body.ToString();
                    var message = JsonSerializer.Deserialize<T>(body, jsonOptions);

                    if (message == null)
                    {
                        await receiver.DeadLetterMessageAsync(rm, "DeserializedToNull", cancellationToken: cancellationToken);
                        continue;
                    }

                    // Validate data annotations if present on T
                    var validationContext = new ValidationContext(message);
                    Validator.ValidateObject(message, validationContext, validateAllProperties: true);

                    result.Add(message);

                    await receiver.CompleteMessageAsync(rm, cancellationToken);
                }
                catch (ValidationException ex)
                {
                    await receiver.DeadLetterMessageAsync(
                        rm,
                        "ValidationError",
                        ex.Message,
                        cancellationToken);
                }
                catch (JsonException)
                {
                    await receiver.DeadLetterMessageAsync(rm, "DeserializationFailed", cancellationToken: cancellationToken);
                }
                catch
                {
                    // Don't complete the message. Let Service Bus retry it.
                    throw;
                }
            }

            return result;
        }

        #endregion

        #region Push Processing

        public async Task StartProcessorAsync(
            string topicName,
            string subscriptionName,
            Func<ProcessMessageEventArgs, Task> messageHandler,
            Func<ProcessErrorEventArgs, Task> errorHandler)
        {
            //if (_processors.ContainsKey(queueName))
            //    return;

            var key = $"{topicName}/{subscriptionName}";
            if (_processors.ContainsKey(key))
                return;

            ServiceBusProcessor processor = _client.CreateProcessor(topicName, subscriptionName, _serviceBusProcessorOptions);

            processor.ProcessMessageAsync += messageHandler;
            processor.ProcessErrorAsync += errorHandler;

            if (_processors.TryAdd(key, processor))
            {
                await processor.StartProcessingAsync();
            }
        }

        public async Task StopProcessorAsync(string topicName, string subscriptionName)
        {
            var key = $"{topicName}/{subscriptionName}";

            if (_processors.TryRemove(key, out ServiceBusProcessor? processor))
            {
                await processor.StopProcessingAsync();
                await processor.DisposeAsync();
            }
        }

        public async Task StartQueueProcessorAsync(
            string queueName,
            Func<ProcessMessageEventArgs, Task> messageHandler,
            Func<ProcessErrorEventArgs, Task> errorHandler)
        {
            if (_processors.ContainsKey(queueName))
                return;

            ServiceBusProcessor processor = _client.CreateProcessor(queueName, _serviceBusProcessorOptions);

            processor.ProcessMessageAsync += messageHandler;
            processor.ProcessErrorAsync += errorHandler;

            if (_processors.TryAdd(queueName, processor))
            {
                await processor.StartProcessingAsync();
            }
        }

        public async Task StopQueueProcessorAsync(string queueName)
        {
            if (_processors.TryRemove(queueName, out ServiceBusProcessor? processor))
            {
                await processor.StopProcessingAsync();
                await processor.DisposeAsync();
            }
        }

        #endregion

        #region Cleanup

        public async ValueTask DisposeAsync()
        {
            foreach (var processor in _processors.Values)
            {
                try
                {
                    await processor.StopProcessingAsync();
                }
                finally
                {
                    await processor.DisposeAsync();
                }
            }
            _processors.Clear();
        }

        #endregion
    }
}
