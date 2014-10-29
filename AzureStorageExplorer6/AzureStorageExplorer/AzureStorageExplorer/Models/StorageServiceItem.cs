using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Blob;
using System.Collections.ObjectModel;

namespace AzureStorageExplorer
{
    public class StorageServiceItem
    {
        public int ItemType { get; set; }
        public String ItemName { get; set; }
        public BlobContainerPermissions Permissions { get; set; } // Optional

        private ObservableCollection<OutlineItem> items = new ObservableCollection<OutlineItem>();
        public ObservableCollection<OutlineItem> Items { get { return items; } }
    }
}
