using System;
using System.Threading.Tasks;
using Vereesa.Core;

namespace Vereesa.ConsoleApp
{
    class Program
    {
        static void Main(string[] args) => MainAsync(args).GetAwaiter().GetResult();
        
        static async Task MainAsync(string[] args)
        {
            var keepRunning = true;
            var client = new VereesaClient();
            await client.StartupAsync();

            while (keepRunning) {
                var input = Console.ReadLine();
                if (input == "exit") {
                    keepRunning = false;
                }
            }
        }
    }
}
