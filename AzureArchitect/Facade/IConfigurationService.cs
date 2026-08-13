using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureServices.Facade
{
    public interface IConfigurationService
    {
        /// <summary>
        /// Returns a bound instance of T for the given section name.
        /// Throws InvalidOperationException if the section is missing or binding fails.
        /// T should have a public parameterless constructor and public settable properties.
        /// </summary>
        T Get<T>(string sectionName) where T : new();

        /// <summary>
        /// Attempts to bind a section to T. Returns true if successful; value is null on failure.
        /// </summary>
        bool TryGet<T>(string sectionName, out T? value) where T : class, new();

        /// <summary>
        /// Binds configuration into an existing instance. Throws if section is missing.
        /// </summary>
        void Bind<T>(string sectionName, T instance) where T : notnull;
    }
}
