using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

            IEnumerable<String> names = GetNames();
            int n = 0;
            String[] nameList = new String[names.Count()];
            foreach (String name in names)
            {
                nameList[n++] = name;
            }

            IEnumerable<Object> values = GetValues();
            int v = 0;
            String[] valueList = new String[values.Count()];
            foreach(Object value in values)
            {
                if (value==null)
                {
                    valueList[v] = "(null)";
                }
                if (value is String)
                {
                    valueList[v] = value as String;
                }
                else
                {
                    valueList[v] = value.ToString();
                }

                Fields.Add(nameList[v], valueList[v]);
                v++;
            }
        }
    }
}
