using System.Collections.Generic;
using Vereesa.Data.Interfaces;

namespace Vereesa.Data.Models.Statistics
{
    public class Statistics : IEntity
    {
        public string Id { get; set; }
        
        public Dictionary<string, object> Stats { get; set; }
        
    }
}