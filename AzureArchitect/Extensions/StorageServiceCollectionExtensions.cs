using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Azure.Storage.Blobs;
using AzureArchitect.Facade;
using AzureArchitect.Services;
using AzureArchitect.Services.BlobStorage;
using AzureServices.Entity;
using AzureServices.Enums;
using AzureServices.Facade;
using AzureServices.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AzureArchitect.Extensions
{
    public static class StorageServiceCollectionExtensions
    {
        public static IServiceCollection AddStorageLibrary(
            this IServiceCollection services,
            IConfiguration configuration)
        {

            if (configuration == null) throw new ArgumentNullException(nameof(configuration));

            var configService = new ConfigurationService(configuration);

            var section = configService.GetSection("Storage");

            services.AddOptions<StorageConfiguration>()
                    .Bind(section)
                    .ValidateOnStart();

            services.AddSingleton<IValidateOptions<StorageConfiguration>, StorageConfigurationValidator>();

            var storageConfiguration = configService.Get<StorageConfiguration>();

            var validator = new StorageConfigurationValidator();
            var validation = validator.Validate(Options.DefaultName, storageConfiguration);

            if (validation.Failed)
                throw new InvalidOperationException("Storage configuration invalid: " + string.Join("; ", validation.Failures));

            var connectionSource = storageConfiguration.ConnectionSource;

            var connectionString = string.Empty;

            switch (connectionSource)
            {
                case nameof(ConnectionSourceEnum.KeyVault):

                    var vaultUri = storageConfiguration.KeyVault?.VaultUri;
                    var secretName = storageConfiguration.KeyVault?.SecretName;
                    var logger = services.BuildServiceProvider().GetRequiredService<ILogger<KeyVaultService>>();
                    try
                    {

                        // Build a KeyVault options instance from the Storage configuration (if present).
                        var keyVaultOptionsInstance = storageConfiguration.KeyVault ?? new KeyVault
                        {
                            VaultUri = null
                        };

                        var options = Options.Create(keyVaultOptionsInstance);

                        var keyVaultService = new KeyVaultService(options, logger);
                        var secretValue = keyVaultService.GetSecretAsync(secretName!).Result.Value;

                        if (string.IsNullOrWhiteSpace(secretValue))
                        {
                            throw new InvalidOperationException("Key Vault Secret Value must be set when ConnectionSource is 'KeyVault'.");
                        }

                        connectionString = secretValue;

                        services.AddSingleton(new BlobServiceClient(connectionString));
                    }
                    catch (AuthenticationFailedException ex)
                    {
                        throw new AuthenticationFailedException($"Authentication failed while getting connection string from keyvault: {ex.Message}");
                    }
                    catch (RequestFailedException rfEx)
                    {
                        throw new InvalidOperationException($"Secret Name '{secretName}' in Key Vault '{vaultUri}' is invalid: {rfEx.Message}");
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException($"Key Vault URI '{vaultUri}' is invalid: {ex.Message}");
                    }
                    break;

                case nameof(ConnectionSourceEnum.AppSettings):

                    connectionString = storageConfiguration.ConnectionString;

                    //Best practice is to use Managed Identity instead of connectionstring
                    if (string.IsNullOrWhiteSpace(connectionString)) throw new ArgumentException("Storage connection string is not configured.");
                    
                        services.AddSingleton(new BlobServiceClient(connectionString));

                    break;

                case nameof(ConnectionSourceEnum.ManagedIdentity):

                        var credentialOptions = new DefaultAzureCredentialOptions
                        {
                            // When running locally in Development, avoid Managed Identity to make it working
                            ExcludeManagedIdentityCredential = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development"
                        };

                        var credential = new DefaultAzureCredential(credentialOptions);

                        services.AddSingleton(new BlobServiceClient(new Uri(storageConfiguration.Uri!), credential));
                    break;
            }
            
            services.AddSingleton<IBlobService, BlobService>();
            services.AddSingleton<StorageConfiguration>();

            return services;
        }
    }
}
