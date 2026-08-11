using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FunctionApps.Functions.Model
{
    public class InventoryMonitorRequest
    {
        public int ProductId { get; set; }

        public string Email { get; set; } = "";
    }
}
