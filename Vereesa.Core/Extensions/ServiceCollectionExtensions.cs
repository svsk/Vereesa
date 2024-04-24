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
        // Find all types that implement the interface IBotModule
        public static IList<Type> GetBotModules() =>
            AppDomain.CurrentDomain
                .GetAssemblies()
                .Where(a => !a.IsDynamic)
                .SelectMany(a => a.GetTypes())
                .Where(t => t.IsClass && !t.IsAbstract && t.GetInterfaces().Contains(typeof(IBotModule)))
                .ToList();

        public static IServiceCollection AddBotServices(this IServiceCollection services)
        {
            foreach (var moduleType in GetBotModules())
            {
                var botModuleType = typeof(DiscordBotModule<>).MakeGenericType(moduleType);

                // Add the type as singleton
                services.AddSingleton(moduleType);
                services.AddSingleton(botModuleType);
            }

            return services;
        }

        private static async Task ClearCommandsAsync(DiscordSocketClient discord)
        {
            foreach (var guild in discord.Guilds)
            {
                await guild.DeleteApplicationCommandsAsync();
            }
        }

        public static void UseBotServices(this IServiceProvider serviceProvider)
        {
            var discord = serviceProvider.GetRequiredService<DiscordSocketClient>();

            discord.Ready += async () =>
            {
                // Clear all existing commands before the BotServices register their current ones.
                await ClearCommandsAsync(discord);
            };

            foreach (var moduleType in GetBotModules())
            {
                try
                {
                    // Resolve all services using DiscordBotService<T>
                    var botModuleType = typeof(DiscordBotModule<>).MakeGenericType(moduleType);
                    serviceProvider.GetRequiredService(botModuleType);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to resolve service {moduleType.Name}: {ex.Message}");
                }
            }
        }
    }
}
