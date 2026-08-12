using Azure.Core;
using Azure.Messaging.ServiceBus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureServices.Entity
{
    public class ServiceBusConfiguration
    {
        /// <summary>
        /// Name of the Service Bus config to identify among multiple Service Bus.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Source for the Service Bus connection (e.g. "KeyVault" or "ConnectionString").
        /// </summary>
        public string? ConnectionSource { get; set; }

        /// <summary>
        /// KeyVault configuration (when ConnectionSource = "KeyVault").
        /// </summary>
        public KeyVault? KeyVault { get; set; }

        /// <summary>
        /// Connection string (when ConnectionSource = "ConnectionString").
        /// </summary>
        public string? ConnectionString { get; set; }

        /// <summary>
        /// Retry policy options for pull receive.
        /// </summary>
        public RetryOptions? RetryOptions { get; set; }

        /// <summary>
        /// Processor (receiver) options for push receive.
        /// </summary>
        public ServiceBusProcessorOptions? ServiceBusProcessorOptions { get; set; }
    }
}
