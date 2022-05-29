using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Vereesa.Core.Infrastructure;

namespace Vereesa.Core.Extensions
{
	public static class BotServices
	{
		public static IList<Type> GetBotServices() => AppDomain.CurrentDomain.GetAssemblies().Where(a => !a.IsDynamic)
			.SelectMany(a => a.GetTypes())
			.Where(myType => myType.IsClass && !myType.IsAbstract && myType.IsSubclassOf(typeof(BotServiceBase)))
			.ToList();

		public static IServiceCollection AddBotServices(this IServiceCollection services)
		{
			foreach (var serviceType in GetBotServices())
			{
				services.AddSingleton(serviceType);
			}

			return services;
		}
	}
}