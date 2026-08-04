using Azure.Messaging.ServiceBus;
using AzureServices.Entity;
using AzureServices.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureServices.Extensions
{
    public static class ServiceBusDataExtension
    {
        private static readonly HashSet<string> validDepartments =
            Enum.GetNames(typeof(ValidDivisionsEnum)).ToHashSet(StringComparer.OrdinalIgnoreCase);

        public static bool IsDepartmentValid(this ServiceBusData message)
        {
            if (message is null || string.IsNullOrEmpty(message.Department))
                return false;

            return validDepartments.Contains(message.Department);
        }
    }
}
