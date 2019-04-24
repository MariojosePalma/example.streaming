using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Example.Streaming.Web.Helpers.Upload.Models
{
    public class AzureStorageConfigModel
    {

        public string AccountName { get; set; }
        public string AccountKey { get; set; }
        public string QueueName { get; set; }
        public string ContainerName { get; set; }


    }
}
