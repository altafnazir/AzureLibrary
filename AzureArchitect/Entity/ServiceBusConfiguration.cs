using Azure.Messaging.ServiceBus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureServices.Entity
{
    public class ServiceBusConfiguration: AzureConfiguration
    {
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
