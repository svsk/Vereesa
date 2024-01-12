using System;

namespace Vereesa.Core.Infrastructure
{
    [AttributeUsage(System.AttributeTargets.Method, AllowMultiple = false)]
    public class OnReadyAttribute : Attribute { }
}
