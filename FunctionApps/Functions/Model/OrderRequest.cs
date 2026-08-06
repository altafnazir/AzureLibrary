using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FunctionApps.Functions.Model
{
    public class OrderRequest
    {
        public Guid OrderId { get; set; }

        public string CustomerId { get; set; } = "";

        public decimal Amount { get; set; }
    }
}
