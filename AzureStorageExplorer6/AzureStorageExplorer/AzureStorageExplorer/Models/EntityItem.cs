using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureStorageExplorer
{
    public class EntityItem
    {
        public String RowKey { get; set; }
        public String PartitionKey { get; set; }
        public String[] Names { get; set; }
        public String[] Values { get; set; }
        public Dictionary<String, String> Fields { get; set; }

        public EntityItem(ElasticTableEntity entity)
        {
            this.RowKey = entity.RowKey;
            this.PartitionKey = entity.PartitionKey;

            this.Fields = new Dictionary<string, string>();

            IEnumerable<String> names = entity.GetNames();
            int n = 0;
            this.Names = new String[names.Count()];
            foreach(String name in names)
            {
                this.Names[n++] = name;
            }

            IEnumerable<Object> values = entity.GetValues();
            int v = 0;
            this.Values = new String[values.Count()];
            foreach(Object value in values)
            {
                if (value==null)
                {
                    this.Values[v] = "(null)";
                }
                if (value is String)
                {
                    this.Values[v] = value as String;
                }
                else
                {
                    this.Values[v] = value.ToString();
                }
                Fields.Add(this.Names[v], this.Values[v]);
                v++;
            }
        }
    }
}
