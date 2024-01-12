using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Vereesa.Core.Infrastructure;

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
                var botServiceType = typeof(BotServiceBase<>).MakeGenericType(serviceType);

                // Add the type as singleton
                services.AddSingleton(serviceType);
                services.AddSingleton(botServiceType);
            }

            return services;
        }

        public static void UseBotServices(this IServiceProvider serviceProvider)
        {
            foreach (var serviceType in GetBotServices())
            {
                try
                {
                    // Resolve all services using BotServiceBase<T>
                    var botServiceType = typeof(BotServiceBase<>).MakeGenericType(serviceType);
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
