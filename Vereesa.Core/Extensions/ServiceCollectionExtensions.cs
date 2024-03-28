using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Vereesa.Core.Discord;

namespace Vereesa.Core.Extensions
{
    public static class BotServices
    {
        // Find all types that implement the interface IBotService
        public static IList<Type> GetBotServices() =>
            AppDomain.CurrentDomain
                .GetAssemblies()
                .Where(a => !a.IsDynamic)
                .SelectMany(a => a.GetTypes())
                .Where(t => t.IsClass && !t.IsAbstract && t.GetInterfaces().Contains(typeof(IBotService)))
                .ToList();

        public static IServiceCollection AddBotServices(this IServiceCollection services)
        {
            foreach (var serviceType in GetBotServices())
            {
                var botServiceType = typeof(DiscordBotService<>).MakeGenericType(serviceType);

                // Add the type as singleton
                services.AddSingleton(serviceType);
                services.AddSingleton(botServiceType);
            }

            return services;
        }

        private static async Task ClearCommandsAsync(DiscordSocketClient discord)
        {
            foreach (var guild in discord.Guilds)
            {
                var existingCommands = await guild.GetApplicationCommandsAsync();
                foreach (var existingCommand in existingCommands)
                {
                    await existingCommand.DeleteAsync();
                }
            }
        }

        public static void UseBotServices(this IServiceProvider serviceProvider)
        {
            var discord = serviceProvider.GetRequiredService<DiscordSocketClient>();

            // Clear all existing commands before the BotServices register their current ones.
            ClearCommandsAsync(discord).GetAwaiter().GetResult();

            foreach (var serviceType in GetBotServices())
            {
                try
                {
                    // Resolve all services using DiscordBotService<T>
                    var botServiceType = typeof(DiscordBotService<>).MakeGenericType(serviceType);
                    serviceProvider.GetRequiredService(botServiceType);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to resolve service {serviceType.Name}: {ex.Message}");
                }
            }
        }
    }
}
