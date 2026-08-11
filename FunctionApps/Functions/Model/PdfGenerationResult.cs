using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FunctionApps.Functions.Model
{
    public class PdfGenerationResult
    {
        public int InvoiceId { get; set; }

        public bool Success { get; set; }

        public string? BlobUrl { get; set; }

        public string? ErrorMessage { get; set; }
    }
}
