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
        public const String NULL_VALUE = "NULL ";

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

            // Add row key, partition key, and timestamp to the fields collection.

            this.Fields.Add("PartitionKey", entity.PartitionKey);
            this.Fields.Add("RowKey", entity.RowKey);
            this.Fields.Add("Timestamp", entity.Timestamp.ToString());

            // Add each entity property to the fields collection.

            foreach (KeyValuePair<String, EntityProperty> prop in entity.Properties)
            {
                if (prop.Value == null || prop.Value.PropertyAsObject == null)
                {
                    this.Fields.Add(prop.Key, NULL_VALUE);
                }
                else
                {
                    this.Fields.Add(prop.Key, prop.Value.PropertyAsObject.ToString());
                }
            }

        }

        // Add any missing fields to the field collection.

        public void AddMissingFields(Dictionary<String, bool> TableColumnNames = null)
        {
            if (TableColumnNames != null)
            {
                foreach (KeyValuePair<String, bool> col in TableColumnNames)
                {
                    if (col.Value && !Fields.ContainsKey(col.Key))
                    {
                        this.Fields.Add(col.Key, NULL_VALUE);
                    }
                }
            }
        }
    }
}
