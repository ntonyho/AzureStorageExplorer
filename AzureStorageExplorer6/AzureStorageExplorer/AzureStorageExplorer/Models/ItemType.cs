using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureStorageExplorer
{
    public class ItemType
    {
        public const int BLOB_SERVICE = 100;        // Blob service node
        public const int BLOB_CONTAINER = 101;      // Blob container (container)

        public const int QUEUE_SERVICE = 200;       // Queue service node
        public const int QUEUE_CONTAINER = 201;       // Queue container (queue)

        public const int TABLE_SERVICE = 300;       // Table service node
        public const int TABLE_CONTAINER = 301;     // Table container (table)
    }
}
