

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
        private DiscordSocketClient _discord;

        public VereesaClient()
        {
            StartupAsync().GetAwaiter().GetResult();
        }

        public async Task StartupAsync()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("config.json", optional: false, reloadOnChange: true)
                .AddJsonFile("config.Local.json", optional: true, reloadOnChange: true);

            _config = builder.Build();

            _discord = new DiscordSocketClient(new DiscordSocketConfig
            {
                LogLevel = LogSeverity.Verbose,
                MessageCacheSize = 1000
            });

            var services = new ServiceCollection()
                .AddSingleton(_config)
                .AddSingleton(_discord)
                .AddSingleton<StartupService>()
                .AddSingleton<GameTrackerService>();

            _serviceProvider = services.BuildServiceProvider();
            await _serviceProvider.GetRequiredService<StartupService>().StartAsync();
            _serviceProvider.GetRequiredService<GameTrackerService>();
        }

        public void Shutdown() 
        {
            _discord.LogoutAsync().GetAwaiter().GetResult();
        }
    }
}