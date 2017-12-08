

using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Vereesa.Core.Services;

namespace Vereesa.Core
{
    public class VereesaClient
    {
        private IConfigurationRoot _config;
        private IServiceProvider _serviceProvider;

        public VereesaClient()
        {
            StartupAsync().GetAwaiter().GetResult();
        }

        public async Task StartupAsync()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("config.json", optional: true, reloadOnChange: true)
                .AddJsonFile("config.Local.json", optional: true);

            _config = builder.Build();

            var discordClient = new DiscordSocketClient(new DiscordSocketConfig
            {
                LogLevel = LogSeverity.Verbose,
                MessageCacheSize = 1000
            });

            var services = new ServiceCollection()
                .AddSingleton(discordClient)
                .AddSingleton<StartupService>()
                .AddSingleton<GameTrackerService>()
                .AddSingleton(_config);

            _serviceProvider = services.BuildServiceProvider();
            
            await _serviceProvider.GetRequiredService<StartupService>().StartAsync();
            _serviceProvider.GetRequiredService<GameTrackerService>();

            await Task.Delay(-1);
        }
    }
}