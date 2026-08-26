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
    public class StorageConfigurationValidator : AzureConfigurationValidator, IValidateOptions<StorageConfiguration>
    {
        public ValidateOptionsResult Validate(string? name, StorageConfiguration serviceBusConfiguration)
        {
            if (serviceBusConfiguration == null)
            {
                return ValidateOptionsResult.Fail("Storage configuration section is missing or has invalid structure.");
            }

            var baseValidationOptionResult = base.Validate(name, serviceBusConfiguration);

            var failures = baseValidationOptionResult.Failures?.ToList();

            if (failures?.Count > 0)
            {
                return ValidateOptionsResult.Fail(failures);
            }

            return ValidateOptionsResult.Success;
        }
    }
}
