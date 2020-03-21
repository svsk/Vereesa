using System.Collections.Generic;
using System.ComponentModel;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Vereesa.Data.Interfaces;

namespace Vereesa.Data.Models.Statistics
{
    public class Statistics : IEntity
    {
        public string Id { get; set; }
        
        public Dictionary<string, object> Stats { get; set; } = new Dictionary<string, object>();

        public void Upsert(string key, object value) 
        {
            if (Stats.ContainsKey(key)) 
            {
                Stats[key] = value;
            } 
            else 
            {
                Stats.Add(key, value);
            }
        }
        
        public T Get<T>(string key) 
        {
            T result = default;

            if (Stats.TryGetValue(key, out var item)) 
            {
                if (item is JToken) 
                {
                    result = JsonConvert.DeserializeObject<T>(item.ToString());
                }
                else 
                {
                    result = (T)TypeDescriptor.GetConverter(typeof(T)).ConvertFrom(item.ToString());
                }
            }

            return result;
        }
    }
}