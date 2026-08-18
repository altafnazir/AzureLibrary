using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.Security.KeyVault.Keys;
using Azure.Security.KeyVault.Secrets;
using AzureServices.Entity;
using AzureServices.Facade;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureServices.Services
{
    public class KeyVaultService : IKeyVaultService
    {
        private readonly KeyVault _config;
        private readonly SecretClient _secretClient;
        private readonly KeyClient _keyClient;
        private readonly ILogger<KeyVaultService> _logger;

        public KeyVaultService(IOptions<KeyVault> options, ILogger<KeyVaultService> logger)
        {
            _config = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _logger = logger;

            if (string.IsNullOrWhiteSpace(_config.VaultUri))
                throw new InvalidOperationException("KeyVault:VaultUri must be configured.");

            if (!Uri.TryCreate(_config.VaultUri, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
                throw new InvalidOperationException($"Invalid Key Vault URI '{_config.VaultUri}'. It must be an absolute HTTPS URI.");

            var credentialOptions = new DefaultAzureCredentialOptions
            {
                // When running locally in Development, avoid Managed Identity probing which can add latency
                ExcludeManagedIdentityCredential = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development"
            };

            var credential = new DefaultAzureCredential(credentialOptions);

            _secretClient = new SecretClient(uri, credential);
            _keyClient = new KeyClient(uri, credential);
        }

        public async Task<(bool IsValid, string? ErrorMessage)> ValidateAsync(string? secretName = null, CancellationToken cancellationToken = default)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(secretName))
                {
                    await _secretClient.GetSecretAsync(secretName, cancellationToken: cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    await foreach (var _ in _secretClient.GetPropertiesOfSecretsAsync(cancellationToken))
                    {
                        break;
                    }
                }
                return (true, null);
            }
            catch (RequestFailedException rfe)
            {
                _logger.LogWarning(rfe, "Key Vault validation failed.");
                return (false, $"RequestFailedException ({rfe.Status}): {rfe.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Key Vault validation failed.");
                return (false, ex.Message);
            }
        }

        public async Task<KeyVaultSecret> GetSecretAsync(string secretName, CancellationToken cancellationToken = default)
        {
            var resp = await _secretClient.GetSecretAsync(secretName, cancellationToken: cancellationToken).ConfigureAwait(false);
            return resp.Value;
        }

        public async Task<KeyVaultSecret> SetSecretAsync(string secretName, string value, CancellationToken cancellationToken = default)
        {
            var resp = await _secretClient.SetSecretAsync(secretName, value, cancellationToken: cancellationToken).ConfigureAwait(false);
            return resp.Value;
        }

        public async Task<DeleteSecretOperation> DeleteSecretAsync(string secretName, CancellationToken cancellationToken = default)
        {
            var operation = await _secretClient.StartDeleteSecretAsync(secretName, cancellationToken).ConfigureAwait(false);
            return operation;
        }

        public async Task<KeyVaultKey> GetKeyAsync(string keyName, CancellationToken cancellationToken = default)
        {
            var resp = await _keyClient.GetKeyAsync(keyName, cancellationToken: cancellationToken).ConfigureAwait(false);
            return resp.Value;
        }

        public async Task<KeyVaultKey> CreateRsaKeyAsync(string keyName, int rsaKeySize = 2048, CancellationToken cancellationToken = default)
        {
            var createOptions = new CreateRsaKeyOptions(keyName, hardwareProtected: false) { KeySize = rsaKeySize };
            var resp = await _keyClient.CreateRsaKeyAsync(createOptions, cancellationToken).ConfigureAwait(false);
            return resp.Value;
        }
    }
}
