using System;
using System.ComponentModel;
using System.Windows.Data;

using Microsoft.WindowsAzure.Storage.Blob;
using System.Collections.ObjectModel;

namespace AzureStorageExplorer
{
    public class StorageServiceItem
    {
        private string filter;

        public int ItemType { get; set; }
        public String ItemName { get; set; }
        public BlobContainerPermissions Permissions { get; set; } // Optional

        private ObservableCollection<OutlineItem> items = new ObservableCollection<OutlineItem>();
        public ObservableCollection<OutlineItem> Items { get { return items; } }

        public ICollectionView FilteredItems
        {
            get
            {
                var source = CollectionViewSource.GetDefaultView(this.Items);
                source.Filter = item => this.FilterItem(item);
                return source;
            }
        }

        public void SetFilter(string filter)
        {
            this.filter = filter;
            this.FilteredItems.Refresh();
        }

        private bool FilterItem(object item)
        {
            if (string.IsNullOrEmpty(this.filter))
            {
                return true;
            }

            OutlineItem outlineItem = item as OutlineItem;
            if (outlineItem != null)
            {
                string displayedText = null;
                if (!string.IsNullOrEmpty(outlineItem.Container))
                {
                    displayedText = outlineItem.Container;
                }
                else if (!string.IsNullOrEmpty(outlineItem.ItemName))
                {
                    displayedText = outlineItem.ItemName;
                }
                
                return !string.IsNullOrEmpty(displayedText) && displayedText.IndexOf(this.filter, StringComparison.OrdinalIgnoreCase) != -1;
            }

            return true;
        }
    }
}
