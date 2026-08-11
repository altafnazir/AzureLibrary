using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FunctionApps.Functions.Model
{
    public class RetryPolicySettings
    {
        public int MaxAttempts { get; set; }

        public TimeSpan FirstRetryInterval { get; set; }

        public TimeSpan MaxRetryInterval { get; set; }

        public TimeSpan RetryTimeout { get; set; }

        public double BackoffCoefficient { get; set; }
    }
}
