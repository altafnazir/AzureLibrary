using System;
using System.Threading.Tasks;

namespace AzureArchitect.Abstractions
{
    public interface ISubscriber : System.IDisposable
    {
        /// <summary>
        /// Start processing messages for the specified topic/subscription.
        /// Handler receives IMessage. Return/throwing exceptions will not automatically complete messages.
        /// </summary>
        Task StartAsync(string topicName, string subscriptionName, Func<IMessage, System.Threading.Tasks.Task> handler);

        /// <summary>
        /// Stops processing and disposes underlying resources.
        /// </summary>
        Task StopAsync();
    }
}