using System;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.Security.KeyVault.Keys;
using Azure.Security.KeyVault.Secrets;

namespace AzureServices.Entity
{
    public class KeyVault
    {
        /// <summary>
        /// The URI of the Key Vault
        /// </summary>
        public string? VaultUri { get; set; }

        /// <summary>
        /// The SecretName for service bus
        /// </summary>
        public string? SecretName { get; set; }
    }
}
