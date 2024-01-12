using System;

namespace Vereesa.Core.Infrastructure
{
    [AttributeUsage(System.AttributeTargets.Method, AllowMultiple = false)]
    public class OnIntervalAttribute : Attribute
    {
        public int Minutes { get; set; }
    }
}
