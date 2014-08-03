using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Table;

namespace AzureStorageExplorer
{
    public class EntityItem : ElasticTableEntity
    {
        public Dictionary<String, String> Fields { get; set; }

        // Create an EntityItem from an ElasticTableEntity.

        public EntityItem(ElasticTableEntity entity)
        {
            this.Properties = entity.Properties;
            this.PartitionKey = entity.PartitionKey;
            this.RowKey = entity.RowKey;
            this.Timestamp = entity.Timestamp;
            this.ETag = entity.ETag;

            // Create and populate Fields dictionary, used for data binding.

            this.Fields = new Dictionary<string, string>();

            this.Fields.Add("RowKey", entity.RowKey);
            this.Fields.Add("PartitionKey", entity.PartitionKey);

            foreach (KeyValuePair<String, EntityProperty> prop in entity.Properties)
            {
                this.Fields.Add(prop.Key, prop.Value.PropertyAsObject.ToString());
            }
        }
    }
}
