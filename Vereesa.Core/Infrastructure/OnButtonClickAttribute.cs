using System;

namespace Vereesa.Core.Infrastructure
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class OnButtonClickAttribute : Attribute
    {
        public string ButtonId { get; }

        public OnButtonClickAttribute(string buttonId)
        {
            ButtonId = buttonId;
        }
    }
}
