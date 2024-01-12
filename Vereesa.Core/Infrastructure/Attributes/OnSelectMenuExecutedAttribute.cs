using System;

namespace Vereesa.Core.Infrastructure
{
    public class OnSelectMenuExecutedAttribute : Attribute
    {
        public string CustomId { get; }

        public OnSelectMenuExecutedAttribute(string customId)
        {
            CustomId = customId;
        }
    }
}
