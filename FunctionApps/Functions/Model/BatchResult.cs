using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FunctionApps.Functions.Model
{
    public class BatchResult
    {
        public string ZipUrl { get; set; } = "";

        public int SuccessCount { get; set; }

        public int FailedCount { get; set; }

        public List<int> FailedInvoices { get; set; } = [];
    }
}
