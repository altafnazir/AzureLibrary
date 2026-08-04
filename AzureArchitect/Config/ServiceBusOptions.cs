using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureArchitect.Config
{
    public class ServiceBusOptions
    {
        public const string SectionName = "ServiceBus";

        /// <summary>
        /// Connection String authentication (optional if FullyQualifiedNamespace is provided).
        /// </summary>
        public string? ConnectionString { get; set; }

        /// <summary>
        /// Fully qualified namespace (e.g., "your-namespace.servicebus.windows.net").
        /// Required for Managed Identity / Entra ID authentication.
        /// </summary>
        public string? FullyQualifiedNamespace { get; set; }
    }
}
