using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureServices.Entity
{
    public class FileToUpload
    {
        public string FileName { get; set; }
        public Stream Content { get; set; }
    }
}
