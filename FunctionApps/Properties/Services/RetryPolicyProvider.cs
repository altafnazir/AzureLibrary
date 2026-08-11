using DurableTask.Core;
using FunctionApps.Enum;
using FunctionApps.Facade;
using FunctionApps.Functions.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FunctionApps.Properties.Services
{
    public class RetryPolicyProvider : IRetryPolicyProvider
    {
        private readonly IConfiguration _configuration;
        private readonly RetryPolicySettings _settings;


        public RetryPolicyProvider(IConfiguration configuration, IOptions<RetryPolicySettings> options)
        {
            _configuration = configuration;
            _settings = options?.Value;
        }

        public RetryOptions Get(RetryPolicyTypeEnum policyName)
        {
            // Get the named subsection under "RetryPolicies"
            var rootSection = _configuration.GetSection("RetryPolicies");
            var section = rootSection?.GetSection(policyName.ToString());

            // If the section does not exist, fall back to injected defaults if present, otherwise throw
            if (section == null || !section.Exists())
            {
                throw new Exception($"Retry policy configuration not found: RetryPolicies:{policyName}.");
            }

            var settings = section.Get<RetryPolicySettings>();

            if (settings == null)
            {
                // Defensive: if binding failed, provide a helpful error
                throw new Exception($"Failed to bind RetryPolicies:{policyName} to {nameof(RetryPolicySettings)}. Check appsettings.json structure and property names.");
            }

            return CreateRetryOptionsFromSettings(settings);
        }

        private static RetryOptions CreateRetryOptionsFromSettings(RetryPolicySettings settings)
        {
            return new RetryOptions(
                settings.FirstRetryInterval,
                settings.MaxAttempts)
            {
                MaxRetryInterval = settings.MaxRetryInterval,
                BackoffCoefficient = settings.BackoffCoefficient,
                RetryTimeout = settings.RetryTimeout
            };
        }
    }
}
