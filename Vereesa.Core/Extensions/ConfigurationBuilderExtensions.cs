using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace Vereesa.Core.Extensions
{
    public static class ConfigurationBuilderExtensions
    {
        public static IConfigurationBuilder AddEnvironmentVariables(this IConfigurationBuilder builder)
        {
            var environmentVariables = Environment.GetEnvironmentVariables();
            var list = new List<KeyValuePair<string, string>>();

            foreach (var key in environmentVariables.Keys)
            {
                list.Add(new KeyValuePair<string, string>(key.ToString(), environmentVariables[key].ToString()));
            }

            builder.AddInMemoryCollection(list);
            return builder;
        }
    }
}