using System;

namespace Vereesa.Core.Infrastructure
{
	public class OnCommandAttribute : Attribute
	{
		public string Command { get; }
		public StringComparison StringComparison { get; set; }

		public OnCommandAttribute(string command)
		{
			Command = command;
		}
	}
}