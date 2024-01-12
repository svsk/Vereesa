using System;

namespace Vereesa.Core.Infrastructure
{
	[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
	public class WithArgumentAttribute : Attribute 
	{

		public int ArgumentIndex { get; }
		public string ArgumentName { get; }

		public WithArgumentAttribute(string argumentName, int argumentIndex)
		{
			this.ArgumentIndex = argumentIndex;
			this.ArgumentName = argumentName;
		}
	}
}