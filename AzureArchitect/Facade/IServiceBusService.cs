using Azure.Messaging.ServiceBus;
using AzureServices.Entity;
using System.Text.Json;

namespace AzureArchitect.Facade
{
    public interface IServiceBusService:IAsyncDisposable
    {
        // Push-based Processing
        Task StartProcessorAsync(string topicName, string subscriptionName, Func<ProcessMessageEventArgs, Task> messageHandler, Func<ProcessErrorEventArgs, Task> errorHandler);
        Task StopProcessorAsync(string topicName, string subscriptionName);
        Task StartQueueProcessorAsync(string queueName, Func<ProcessMessageEventArgs, Task> messageHandler, Func<ProcessErrorEventArgs, Task> errorHandler);
        Task StopQueueProcessorAsync(string queueName);
    }
}
