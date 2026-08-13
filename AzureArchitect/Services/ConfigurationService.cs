using AzureServices.Facade;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureServices.Services
{
    public class ConfigurationService : IConfigurationService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<ConfigurationService>? _logger;

        public ConfigurationService(IConfiguration configuration, ILogger<ConfigurationService>? logger = null)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger;
        }

        public T Get<T>(string sectionName) where T : new()
        {
            if (string.IsNullOrWhiteSpace(sectionName))
                throw new ArgumentException("{sectionName} must be provided");

            var section = _configuration.GetSection(sectionName);

            if (!section.Exists())
                throw new InvalidOperationException($"Configuration section '{sectionName}' not found.");

            var bound = section.Get<T>();
            if (bound is null)
            {
                var msg = $"Failed to bind configuration section '{sectionName}' to type {typeof(T).FullName}. Ensure it has a public parameterless constructor and public settable properties.";
                _logger?.LogError(msg);
                throw new InvalidOperationException(msg);
            }

            return bound;
        }

        public bool TryGet<T>(string sectionName, out T? value) where T : class, new()
        {
            value = null;
            if (string.IsNullOrWhiteSpace(sectionName))
                return false;

            var section = _configuration.GetSection(sectionName);
            if (!section.Exists()) return false;

            try
            {
                value = section.Get<T>();
                return value != null;
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Configuration bind failed for section '{Section}' to {Type}", sectionName, typeof(T).FullName);
                return false;
            }
        }

        public void Bind<T>(string sectionName, T instance) where T : notnull
        {
            if (instance is null) throw new ArgumentNullException(nameof(instance));
            if (string.IsNullOrWhiteSpace(sectionName)) throw new ArgumentException("sectionName must be provided", nameof(sectionName));

            var section = _configuration.GetSection(sectionName);
            if (!section.Exists())
                throw new InvalidOperationException($"Configuration section '{sectionName}' not found.");

            section.Bind(instance);
        }
    }
}
