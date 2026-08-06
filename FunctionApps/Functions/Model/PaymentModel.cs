using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FunctionApps.Functions.Model
{
    public class PaymentModel
    {
        public string InstanceId { get; set; } = string.Empty;

        public string PaymentId { get; set; } = string.Empty;

        public decimal Amount { get; set; }

        public bool IsSuccessful { get; set; }
    }
}
