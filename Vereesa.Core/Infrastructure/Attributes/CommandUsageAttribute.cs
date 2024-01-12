using System;

namespace Vereesa.Core.Infrastructure
{
	public class CommandUsageAttribute : Attribute
	{
		public CommandUsageAttribute(string usageDescription)
		{
			UsageDescription = usageDescription;
		}

		public string UsageDescription { get; }
	}
}