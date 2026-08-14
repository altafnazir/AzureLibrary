using Azure;
using Azure.Identity;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Azure.Security.KeyVault.Secrets;
using AzureArchitect.Facade;
using AzureArchitect.Services;
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
    public static class ServiceBusServiceCollectionExtensions
    {
        public static IServiceCollection AddServiceBusLibrary(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));

            var section = configuration.GetSection("ServiceBus");

            services.AddOptions<ServiceBusConfiguration>()
                    .Bind(section)
                    .ValidateOnStart();

            services.AddSingleton<IValidateOptions<ServiceBusConfiguration>, ServiceBusConfigurationValidator>();

            var configService = new ConfigurationService(configuration);
            var serviceBusConfiguration = section.Get<ServiceBusConfiguration>();

            if (serviceBusConfiguration == null)
                throw new InvalidOperationException("ServiceBus configuration section is missing or has invalid structure.");

            var validator = new ServiceBusConfigurationValidator();
            var validation = validator.Validate(Options.DefaultName, serviceBusConfiguration);
            
            if (validation.Failed)
                throw new InvalidOperationException("ServiceBus configuration invalid: " + string.Join("; ", validation.Failures));

            var vaultUri = serviceBusConfiguration.KeyVault?.VaultUri;
            var secretName = serviceBusConfiguration.KeyVault?.SecretName;
            var connectionString = string.Empty;

            if (serviceBusConfiguration.ConnectionSource == ConnectionSourceEnum.KeyVault.ToString())
            {
                var credentialOptions = new DefaultAzureCredentialOptions
                {
                    // When running locally in Development, avoid Managed Identity probing which can add latency
                    ExcludeManagedIdentityCredential = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development"
                };

                var credential = new DefaultAzureCredential(credentialOptions);
                var secretClient = new SecretClient(new Uri(vaultUri!), credential);

                try
                {
                    var secretResponse = secretClient.GetSecret(secretName);
                    var secretValue = secretResponse?.Value?.Value;

                    if (string.IsNullOrWhiteSpace(secretValue))
                    {
                        throw new InvalidOperationException("Key Vault Secret Value must be set when ConnectionSource is 'KeyVault'.");
                    }

                    connectionString = secretValue;

                    //If need to get from ConnectionString if Key Vault is invalid, then uncomment following
                    //else
                    //{
                    //    if (!string.IsNullOrWhiteSpace(serviceBusConfiguration.ConnectionString))
                    //    {
                    //        connectionString = serviceBusConfiguration.ConnectionString!;
                    //    }
                    //    else
                    //    {
                    //        throw new InvalidOperationException($"Secret '{secretName}' in Key Vault '{vaultUri}' is empty and no ConnectionString is provided.");
                    //    }
                    //}
                }
                catch (AuthenticationFailedException ex)
                {
                    throw new AuthenticationFailedException($"Authentication failed while getting connection string from keyvault: {ex.Message}");
                }
                catch (RequestFailedException rfEx)
                {
                    throw new InvalidOperationException($"Secret Name '{secretName}' in Key Vault '{vaultUri}' is invalid: {rfEx.Message}");

                    //If need to get from ConnectionString if Key Vault is invalid, then uncomment following
                    //if (!string.IsNullOrWhiteSpace(serviceBusConfiguration.ConnectionString))
                    //{
                    //    Console.WriteLine($"Secret '{secretName}' in Key Vault '{vaultUri}' is invalid so trying to get from ConnectionString.");
                    //    connectionString = serviceBusConfiguration.ConnectionString!;
                    //}
                    //else
                    //{
                    //    throw new InvalidOperationException($"Secret '{secretName}' not found in Key Vault '{vaultUri}' and no direct ConnectionString was provided.", rfEx);
                    //}
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Key Vault URI '{vaultUri}' is invalid: {ex.Message}");

                    //If need to get from ConnectionString if Key Vault is invalid, then uncomment following
                    //Console.WriteLine($"Key Vault URI '{vaultUri}' is invalid so trying to get from ConnectionString: {ex.Message}");
                    //connectionString = serviceBusConfiguration.ConnectionString!;
                }
            }
            else
            {
                connectionString = serviceBusConfiguration.ConnectionString;
            }

            if (!services.Any(sd => sd.ServiceType == typeof(IConfigurationService)))
            {
                services.AddSingleton<IConfigurationService>(sp =>
                    new ConfigurationService(configuration, sp.GetService<ILogger<ConfigurationService>>()));
            }

            // Reuse the existing overload that takes a bound ServiceBusConfiguration instance.
            //services.AddServiceBusLibrary(serviceBusConfiguration);

            var retryOptions = serviceBusConfiguration.RetryOptions;
            var clientOptions = new ServiceBusClientOptions { RetryOptions = retryOptions };

            var serviceBusProcessorOptions = serviceBusConfiguration.ProcessorOptions;

            try
            {
                services.AddSingleton(new ServiceBusClient(connectionString, clientOptions));
                services.AddSingleton(new ServiceBusAdministrationClient(connectionString));
            }
            catch (Exception)
            {
                var msg = $"Failed to create Service Bus clients with the configured connection string. " +
                              $"Verify connectionstring for servicebus is valid.";

                Console.WriteLine(msg);

                throw;
            }

            services.AddSingleton(serviceBusProcessorOptions ?? new ServiceBusProcessorOptions());

            services.AddSingleton<ServiceBusService>();

            services.AddSingleton<IMessagingService>(
                sp => sp.GetRequiredService<ServiceBusService>());

            services.AddSingleton<IServiceBusService>(
                sp => sp.GetRequiredService<ServiceBusService>());

            //services.AddSingleton<IKeyVaultService, KeyVaultService>();

            return services;
        }

        /*
        public static IServiceCollection AddServiceBusLibrary(
            this IServiceCollection services,
            ServiceBusConfiguration serviceBusConfiguration)
        {
            var connectionString = string.Empty;

            var connectionSource = serviceBusConfiguration.ConnectionSource;

            if (string.IsNullOrWhiteSpace(serviceBusConfiguration.ConnectionSource))
            {
                throw new InvalidOperationException("ConnectionSource must be provided in appsettings.json.");
            }

            if (connectionSource == ConnectionSourceEnum.KeyVault.ToString())
            {
                if (serviceBusConfiguration.KeyVault == null)
                {
                    throw new InvalidOperationException("KeyVault configuration must be provided when ConnectionSource is 'KeyVault'.");
                }

                var vaultUri = serviceBusConfiguration.KeyVault?.VaultUri;
                var secretName = serviceBusConfiguration.KeyVault?.SecretName;

                if (string.IsNullOrWhiteSpace(vaultUri))
                {
                    throw new InvalidOperationException("Key Vault URI must be provided when ConnectionSource is 'KeyVault'.");
                }

                if (string.IsNullOrWhiteSpace(secretName))
                {
                    throw new InvalidOperationException("Key Vault Secret Name must be provided when ConnectionSource is 'KeyVault'.");
                }

                if (!Uri.TryCreate(vaultUri, UriKind.Absolute, out var vaultUriObj) || vaultUriObj.Scheme != Uri.UriSchemeHttps)
                {
                    throw new InvalidOperationException($"Invalid Key Vault URI '{vaultUri}'. It must be an absolute HTTPS URI.");
                }

                //if (!vaultUriObj.Host.EndsWith(".vault.azure.net", StringComparison.OrdinalIgnoreCase))
                //{
                //    // Either treat as warning or enforce
                //    throw new InvalidOperationException($"Key Vault URI '{vaultUri}' does not appear to be an Azure Key Vault host (expected '*.vault.azure.net').");
                //}

                var credentialOptions = new DefaultAzureCredentialOptions
                {
                    // When running locally in Development, avoid Managed Identity probing which can add latency
                    ExcludeManagedIdentityCredential = true
                };

                var credential = new DefaultAzureCredential(credentialOptions);

                var secretClient = new SecretClient(vaultUriObj, credential);

                try
                {
                    var secretResponse = secretClient.GetSecret(secretName);
                    var secretValue = secretResponse?.Value?.Value;

                    if (string.IsNullOrWhiteSpace(secretValue))
                    {
                        throw new InvalidOperationException("Key Vault Secret Value must be provided when ConnectionSource is 'KeyVault'.");
                    }

                    connectionString = secretValue;

                    //else
                    //{
                    //    if (!string.IsNullOrWhiteSpace(serviceBusConfiguration.ConnectionString))
                    //    {
                    //        connectionString = serviceBusConfiguration.ConnectionString!;
                    //    }
                    //    else
                    //    {
                    //        throw new InvalidOperationException($"Secret '{secretName}' in Key Vault '{vaultUri}' is empty and no ConnectionString is provided.");
                    //    }
                    //}
                }
                catch (AuthenticationFailedException ex)
                {
                    throw new AuthenticationFailedException($"Authentication failed while getting connection string from keyvault: {ex.Message}");
                }
                catch (RequestFailedException rfEx)
                {
                    throw new InvalidOperationException($"Secret Name '{secretName}' in Key Vault '{vaultUri}' is invalid: {rfEx.Message}");

                    //If need to get from ConnectionString if Key Vault is invalid, then uncomment following
                    //if (!string.IsNullOrWhiteSpace(serviceBusConfiguration.ConnectionString))
                    //{
                    //    Console.WriteLine($"Secret '{secretName}' in Key Vault '{vaultUri}' is invalid so trying to get from ConnectionString.");
                    //    connectionString = serviceBusConfiguration.ConnectionString!;
                    //}
                    //else
                    //{
                    //    throw new InvalidOperationException($"Secret '{secretName}' not found in Key Vault '{vaultUri}' and no direct ConnectionString was provided.", rfEx);
                    //}
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Key Vault URI '{vaultUri}' is invalid: {ex.Message}");

                    //If need to get from ConnectionString if Key Vault is invalid, then uncomment following
                    //Console.WriteLine($"Key Vault URI '{vaultUri}' is invalid so trying to get from ConnectionString: {ex.Message}");
                    //connectionString = serviceBusConfiguration.ConnectionString!;
                }
            }
            else
            {
                connectionString = serviceBusConfiguration.ConnectionString;
            }

            if (string.IsNullOrWhiteSpace(connectionString))
                throw new InvalidOperationException("Service Bus connection string is not configured. Set ServiceBus:ConnectionString in appsettings.json.");

            var retryOptions = serviceBusConfiguration.RetryOptions;

            var clientOptions = new ServiceBusClientOptions { RetryOptions = retryOptions };

            var serviceBusProcessorOptions = serviceBusConfiguration.ProcessorOptions;

            try
            {
                services.AddSingleton(new ServiceBusClient(connectionString, clientOptions));
                services.AddSingleton(new ServiceBusAdministrationClient(connectionString));
            }
            catch (Exception)
            {
                var msg = $"Failed to create Service Bus clients with the configured connection string. " +
                              $"Verify connectionstring for servicebus is valid.";

                Console.WriteLine(msg);

                throw;
            }

            services.AddSingleton(serviceBusProcessorOptions ?? new ServiceBusProcessorOptions());

            services.AddSingleton<ServiceBusService>();

            services.AddSingleton<IMessagingService>(
                sp => sp.GetRequiredService<ServiceBusService>());

            services.AddSingleton<IServiceBusService>(
                sp => sp.GetRequiredService<ServiceBusService>());

            //services.AddSingleton<IKeyVaultService, KeyVaultService>();

            return services;
        }*/
    }
}
