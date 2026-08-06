using System;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace FunctionApps.Functions
{
    public class TopicSubscription
    {
        private readonly ILogger<TopicSubscription> _logger;

        public TopicSubscription(ILogger<TopicSubscription> logger)
        {
            _logger = logger;
        }

        [Function(nameof(HRSubscription))]
        public async Task HRSubscription(
            [ServiceBusTrigger("events-topic", "HRSubscription", Connection = "MyServiceBusConnection")]
            ServiceBusReceivedMessage message,
            ServiceBusMessageActions messageActions)
        {
            _logger.LogInformation("Message ID: {id}", message.MessageId);
            _logger.LogInformation("Message Body: {body}", message.Body);
            _logger.LogInformation("Message Content-Type: {contentType}", message.ContentType);

             // Complete the message
            await messageActions.CompleteMessageAsync(message);
        }

        [Function(nameof(ITSubscription))]
        public async Task ITSubscription(
            [ServiceBusTrigger("events-topic", "ITSubscription", Connection = "MyServiceBusConnection")]
            ServiceBusReceivedMessage message,
            ServiceBusMessageActions messageActions)
        {
            _logger.LogInformation("Message ID: {id}", message.MessageId);
            _logger.LogInformation("Message Body: {body}", message.Body);
            _logger.LogInformation("Message Content-Type: {contentType}", message.ContentType);

            // Complete the message
            await messageActions.CompleteMessageAsync(message);
        }

        [Function(nameof(FinanceSubscription))]
        public async Task FinanceSubscription(
            [ServiceBusTrigger("events-topic", "FinanceSubscription", Connection = "MyServiceBusConnection")]
            ServiceBusReceivedMessage message,
            ServiceBusMessageActions messageActions)
        {
            _logger.LogInformation("Message ID: {id}", message.MessageId);
            _logger.LogInformation("Message Body: {body}", message.Body);
            _logger.LogInformation("Message Content-Type: {contentType}", message.ContentType);

            // Complete the message
            await messageActions.CompleteMessageAsync(message);
        }

        [Function(nameof(InvalidDepartment))]
        public async Task InvalidDepartment(
            [ServiceBusTrigger("events-topic", "InvalidDepartment", Connection = "MyServiceBusConnection")]
            ServiceBusReceivedMessage message,
            ServiceBusMessageActions messageActions)
        {
            _logger.LogInformation("Message ID: {id}", message.MessageId);
            _logger.LogInformation("Message Body: {body}", message.Body);
            _logger.LogInformation("Message Content-Type: {contentType}", message.ContentType);

            // Complete the message
            await messageActions.CompleteMessageAsync(message);
        }
    }
}
