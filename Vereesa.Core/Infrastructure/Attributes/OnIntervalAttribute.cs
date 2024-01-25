using System;

namespace Vereesa.Core.Infrastructure
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class OnIntervalAttribute : Attribute
    {
        public int Seconds { get; set; }
        public int Minutes { get; set; }
    }
}
