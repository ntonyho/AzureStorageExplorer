using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureStorageExplorer
{
    public class MessageItem
    {
        public String Id { get; set; }
        public String BytesValue { get; set; }
        public String StringValue { get; set; }
        public String InsertionTime { get; set; }
        public String ExpirationTime { get; set; }
        public String NextVisibleTime { get; set; }
        public int DequeueCount { get; set; }
        public String PopReceipt { get; set; }
        
    }
}
