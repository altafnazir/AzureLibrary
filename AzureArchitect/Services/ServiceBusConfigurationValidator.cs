using AzureServices.Entity;
using AzureServices.Enums;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureServices.Services
{
    public class ServiceBusConfigurationValidator : IValidateOptions<ServiceBusConfiguration>
    {
        public ValidateOptionsResult Validate(string? name, ServiceBusConfiguration options)
        {
            if (options == null)
            {
                return ValidateOptionsResult.Fail("ServiceBus configuration section is missing or has invalid structure.");
            }

            var failures = new List<string>();

            if (string.IsNullOrWhiteSpace(options.ConnectionSource))
            {
                failures.Add("ConnectionSource is required (KeyVault | ConnectionString).");
            }

            if (options.ConnectionSource == ConnectionSourceEnum.KeyVault.ToString())
            {
                if (options.KeyVault == null)
                {
                    failures.Add("KeyVault section is required when ConnectionSource is 'KeyVault'.");
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(options.KeyVault.VaultUri))
                    {
                        failures.Add("KeyVault:VaultUri is required.");
                    }
                    else if (!Uri.TryCreate(options.KeyVault.VaultUri, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
                    {
                        failures.Add("KeyVault:VaultUri must be an absolute HTTPS URI.");
                    }

                    if (string.IsNullOrWhiteSpace(options.KeyVault.SecretName))
                    {
                        failures.Add("KeyVault:SecretName is required when using KeyVault as ConnectionSource.");
                    }
                }
            }
            else
            {
                if (string.IsNullOrWhiteSpace(options.ConnectionString))
                {
                    failures.Add("ConnectionString is required when ConnectionSource is not 'KeyVault'.");
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
