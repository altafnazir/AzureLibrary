using DurableTask.Core;
using FunctionApps.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FunctionApps.Facade
{
    public interface IRetryPolicyProvider
    {
        RetryOptions Get(RetryPolicyTypeEnum policyName);
    }
}
