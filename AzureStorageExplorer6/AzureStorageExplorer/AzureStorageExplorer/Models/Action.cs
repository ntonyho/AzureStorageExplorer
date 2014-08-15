using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureStorageExplorer
{
    public class Action
    {
        public const int ACTION_NONE = 0;
        public const int ACTION_UPLOAD_BLOBS = 1;
        public const int ACTION_DOWNLOAD_BLOBS = 2;
        public const int ACTION_COPY_BLOB = 3;
        public const int ACTION_DELETE_BLOBS = 4;
        public const int ACTION_NEW_CONTAINER = 5;
        public const int ACTION_DELETE_CONTAINER = 6;
        public const int ACTION_CONTAINER_ACCESS_LEVEL = 7;
        public const int ACTION_NEW_TABLE = 8;
        public const int ACTION_DELETE_TABLE = 9;
        public const int ACTION_DOWNLOAD_ENTITIES = 10;
        public const int ACTION_DELETE_ENTITIES = 11;
        public const int ACTION_NEW_QUEUE = 12;
        public const int ACTION_DELETE_QUEUE = 13;
        public const int ACTION_DELETE_MESSAGES = 14;
        public const int ACTION_UPLOAD_ENTITIES = 15;


        public int Id { get; set; }
        public int ActionType { get; set; }
        public bool IsCompleted { get; set; }
        public String Message { get; set; }
    }
}
