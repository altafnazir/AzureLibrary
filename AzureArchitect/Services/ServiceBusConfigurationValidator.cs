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
    public class ServiceBusConfigurationValidator : IValidateOptions<ServiceBusConfiguration>
    {
        public ValidateOptionsResult Validate(string? name, ServiceBusConfiguration serviceBusConfiguration)
        {
            if (serviceBusConfiguration == null)
            {
                return ValidateOptionsResult.Fail("ServiceBus configuration section is missing or has invalid structure.");
            }

            var failures = new List<string>();

            if (string.IsNullOrWhiteSpace(serviceBusConfiguration.ConnectionSource))
            {
                failures.Add("ConnectionSource is required (KeyVault | ConnectionString).");
            }

            if (serviceBusConfiguration.ConnectionSource == ConnectionSourceEnum.KeyVault.ToString())
            {
                if (serviceBusConfiguration.KeyVault == null)
                {
                    failures.Add("KeyVault section is required when ConnectionSource is 'KeyVault'.");
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(serviceBusConfiguration.KeyVault.VaultUri))
                    {
                        failures.Add("KeyVault:VaultUri is required.");
                    }
                    else if (!Uri.TryCreate(serviceBusConfiguration.KeyVault.VaultUri, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
                    {
                        failures.Add("KeyVault:VaultUri must be an absolute HTTPS URI.");
                    }

                    if (string.IsNullOrWhiteSpace(serviceBusConfiguration.KeyVault.SecretName))
                    {
                        failures.Add("KeyVault:SecretName is required when using KeyVault as ConnectionSource.");
                    }
                }
            }
            else if (serviceBusConfiguration.ConnectionSource == ConnectionSourceEnum.AppSettings.ToString())
            {
                if (string.IsNullOrWhiteSpace(serviceBusConfiguration.ConnectionString))
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
