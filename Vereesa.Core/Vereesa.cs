

using System;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Vereesa.Core.Services;

namespace Vereesa.Core 
{
    public class Vereesa 
    {
        public async Task StartupAsync() 
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("config.json", optional: false, reloadOnChange: true)
                .AddJsonFile("config.Local.json", optional: true, reloadOnChange: true);

            var config = builder.Build();

            var services = new ServiceCollection()
                .AddSingleton(new DiscordSocketClient(new DiscordSocketConfig {
                    LogLevel = LogSeverity.Verbose,
                    MessageCacheSize = 1000
                }))
                .AddSingleton<StartupService>()
                .AddSingleton(config);

            var provider = services.BuildServiceProvider();
            await provider.GetRequiredService<StartupService>().StartAsync();

            await Task.Delay(-1);
        }
    }
}