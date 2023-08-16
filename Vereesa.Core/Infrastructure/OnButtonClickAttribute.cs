using System;

namespace Vereesa.Core.Infrastructure
{
    public class OnButtonClickAttribute : Attribute
    {
        public string ButtonId { get; }

        public OnButtonClickAttribute(string buttonId)
        {
            ButtonId = buttonId;
        }
    }
}
