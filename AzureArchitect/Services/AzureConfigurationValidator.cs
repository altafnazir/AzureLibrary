using AzureServices.Entity;
using AzureServices.Enums;
using Microsoft.Azure.Amqp;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureServices.Services
{
    public class AzureConfigurationValidator : IValidateOptions<AzureConfiguration>
    {
        public ValidateOptionsResult Validate(string? name, AzureConfiguration azureConfiguration)
        {
            var failures = new List<string>();

            if (string.IsNullOrWhiteSpace(azureConfiguration.ConnectionSource))
            {
                failures.Add("ConnectionSource is required (KeyVault | ConnectionString).");
            }

            ConnectionSourceEnum connectionSource;

            try
            {
                var isValidConnectionSource = Enum.TryParse<ConnectionSourceEnum>(azureConfiguration.ConnectionSource, ignoreCase: true, out connectionSource);

                if (!isValidConnectionSource)
                {
                    throw new InvalidDataException($"Connection Source is not valid. Valid values are following: {string.Join(", ", Enum.GetNames(typeof(ConnectionSourceEnum)))}");
                }
            }
            catch (Exception)
            {
                throw new InvalidDataException($"Connection Source is not valid. Valid values are following: {string.Join(", ", Enum.GetNames(typeof(ConnectionSourceEnum)))}");
            }

            if (connectionSource == ConnectionSourceEnum.KeyVault)
            {
                if (azureConfiguration.KeyVault == null)
                {
                    failures.Add("KeyVault section is required when ConnectionSource is 'KeyVault'.");
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(azureConfiguration.KeyVault.VaultUri))
                    {
                        failures.Add("KeyVault:VaultUri is required.");
                    }
                    else if (!Uri.TryCreate(azureConfiguration.KeyVault.VaultUri, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
                    {
                        failures.Add("KeyVault:VaultUri must be an absolute HTTPS URI.");
                    }

                    if (string.IsNullOrWhiteSpace(azureConfiguration.KeyVault.SecretName))
                    {
                        failures.Add("KeyVault:SecretName is required when using KeyVault as ConnectionSource.");
                    }
                }
            }
            else if (connectionSource == ConnectionSourceEnum.AppSettings)
            {
                if (string.IsNullOrWhiteSpace(azureConfiguration.ConnectionString))
                {
                    failures.Add("ConnectionString is required when ConnectionSource is not 'KeyVault'.");
                }
            }
            else if(connectionSource == ConnectionSourceEnum.ManagedIdentity)
            {
                if (!Uri.TryCreate(azureConfiguration.Uri, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
                {
                    failures.Add("Uri must be an absolute HTTPS URI.");
                }
            }

            if (failures.Count > 0)
            {
                return ValidateOptionsResult.Fail(failures);
            }

            return ValidateOptionsResult.Success;
        }
    }
}
