using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureServices.Entity
{
    public class BatchMessage<T>
    {
        public T Message { get; set; } = default!;

        public Dictionary<string, object> ApplicationProperties { get; set; }
            = new();
    }
}
