namespace AzureServices.Entity
{
    public class AzureConfiguration
    {
        /// <summary>
        /// Name of the Service Bus.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Uri of the Service Bus.
        /// </summary>
        public string? Uri { get; set; }

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
    }
}