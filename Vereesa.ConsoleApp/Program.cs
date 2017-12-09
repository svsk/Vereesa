using System;
using Vereesa.Core;

namespace Vereesa.ConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            var keepRunning = true;
            var client = new VereesaClient();

            while (keepRunning) {
                var input = Console.ReadLine();
                if (input == "exit") {
                    keepRunning = false;
                }
            }
        }
    }
}
