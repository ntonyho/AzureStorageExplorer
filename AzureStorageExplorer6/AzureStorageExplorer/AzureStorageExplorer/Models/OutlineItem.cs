using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Blob;

namespace AzureStorageExplorer
{
    public class OutlineItem
    {
        public int ItemType { get; set; }
        public String Container { get; set; }
        public String ItemName { get; set; }
        public BlobContainerPermissions Permissions { get; set; }
    }
}
