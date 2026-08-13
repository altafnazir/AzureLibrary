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
        /// Name of the Service Bus.
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
        /// Use ServiceBusRetryOptions from Azure.Messaging.ServiceBus which the binder can construct.
        /// </summary>
        public ServiceBusRetryOptions? RetryOptions { get; set; }

        /// <summary>
        /// Processor (receiver) options for push receive.
        /// The JSON key in appsettings.json is "ProcessorOptions", so name the property the same for easy binding.
        /// </summary>
        public ServiceBusProcessorOptions? ProcessorOptions { get; set; }
    }
}
