using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Vereesa.Core.Infrastructure;

namespace Vereesa.Core.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddBotServices(this IServiceCollection services) 
		{
			var botServiceTypes = Assembly.GetAssembly(typeof(VereesaClient)).GetTypes()
            	.Where(myType => myType.IsClass && !myType.IsAbstract && myType.IsSubclassOf(typeof(BotServiceBase)))
				.ToList();

			foreach (var serviceType in botServiceTypes) 
			{
				services.AddSingleton(serviceType);
			}

			return services;
		}
    }
}