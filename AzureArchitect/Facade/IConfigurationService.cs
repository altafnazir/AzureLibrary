using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureServices.Facade
{
    public interface IConfigurationService
    {
        IConfigurationSection GetSection(string sectionName);

        T Get<T>() where T : new();

        bool TryGet<T>(string sectionName, out T? value) where T : class, new();

        void Bind<T>(string sectionName, T instance) where T : notnull;
    }
}
