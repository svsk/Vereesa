using System;
using System.Threading.Tasks;
using Vereesa.Core;

namespace Vereesa.ConsoleApp
{
	class Program
	{
		static async Task Main(string[] args)
		{
			var keepRunning = true;
			var client = new VereesaClient();
			await client.StartupAsync();

			while (keepRunning)
			{
				var input = Console.ReadLine();
				if (input == "exit")
				{
					keepRunning = false;
				}
			}
		}
	}
}
