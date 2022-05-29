using System;
using System.Threading.Tasks;
using Vereesa.Awdeo;
using Vereesa.Core;

namespace Vereesa.ConsoleApp
{
	class Program
	{
		static async Task Main(string[] args)
		{
			var keepRunning = true;
			var client = new VereesaClient();
			await client.StartupAsync((services, config) =>
			{
				services.AddAwdeo(config);
			});

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
