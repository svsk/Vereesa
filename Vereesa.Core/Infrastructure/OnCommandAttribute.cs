using System;

namespace Vereesa.Core.Infrastructure
{
	public class OnCommandAttribute : Attribute
	{
		public string Command { get; }

		public OnCommandAttribute(string command)
		{
			Command = command;
		}
	}
}
