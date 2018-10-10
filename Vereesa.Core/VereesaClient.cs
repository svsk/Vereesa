using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Timers;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Vereesa.Core.Configuration;
using Vereesa.Core.Services;
using Vereesa.Data;
using Vereesa.Data.Models.Commands;
using Vereesa.Data.Models.Gambling;
using Vereesa.Data.Models.GameTracking;
using Vereesa.Data.Models.Giveaways;
using Vereesa.Data.Repositories;
 
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
            //Set up configuration
            var builder = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("config.json", optional: false, reloadOnChange: true)
                .AddJsonFile("config.Local.json", optional: true, reloadOnChange: true);

            _config = builder.Build();
            
            var discordSettings = new DiscordSettings();
            var battleNetApiSettings = new BattleNetApiSettings();
            var gameStateEmissionSettings = new GameStateEmissionSettings();
            var gamblingSettings = new GamblingSettings();
            var voiceChannelTrackerSettings = new VoiceChannelTrackerSettings();
            var guildApplicationSettings = new GuildApplicationSettings();
            var signupsSettings = new SignupsSettings();

            _config.GetSection(nameof(DiscordSettings)).Bind(discordSettings);
            _config.GetSection(nameof(BattleNetApiSettings)).Bind(battleNetApiSettings);
            _config.GetSection(nameof(GameStateEmissionSettings)).Bind(gameStateEmissionSettings);
            _config.GetSection(nameof(GamblingSettings)).Bind(gamblingSettings);
            _config.GetSection(nameof(VoiceChannelTrackerSettings)).Bind(voiceChannelTrackerSettings);
            _config.GetSection(nameof(GuildApplicationSettings)).Bind(guildApplicationSettings);
            _config.GetSection(nameof(SignupsSettings)).Bind(signupsSettings);

            //Set up discord client
            _discord = new DiscordSocketClient(new DiscordSocketConfig
            {
                LogLevel = LogSeverity.Verbose,
                MessageCacheSize = 1000
            });

            //Set up a service provider with all relevant resources for DI
            IServiceCollection services = new ServiceCollection()
                .AddSingleton(_discord)
                .AddSingleton(discordSettings)
                .AddSingleton(battleNetApiSettings)
                .AddSingleton(gameStateEmissionSettings)
                .AddSingleton(gamblingSettings)
                .AddSingleton(voiceChannelTrackerSettings)
                .AddSingleton(guildApplicationSettings)
                .AddSingleton(signupsSettings)
                .AddSingleton<Random>()
                .AddSingleton<StartupService>()
                .AddSingleton<EventHubService>()
                .AddSingleton<NeonApiService>()
                .AddSingleton<BattleNetApiService>()
                .AddSingleton<GameTrackerService>()
                .AddSingleton<GiveawayService>()
                .AddSingleton<GuildApplicationService>()
                .AddSingleton<GamblingService>()
                .AddSingleton<VoiceChannelTrackerService>()
                .AddSingleton<RoleGiverService>()    
                .AddSingleton<CommandService>()
                .AddSingleton<SignupsService>()
                .AddScoped<JsonRepository<GameTrackMember>>()
                .AddScoped<JsonRepository<Giveaway>>()
                .AddScoped<JsonRepository<GamblingStandings>>()
                .AddScoped<JsonRepository<Command>>()
                .AddLogging(config => { 
                    config.AddConsole();
                });

            //Build the service provider
            _serviceProvider = services.BuildServiceProvider();

            //Start the desired services
            _serviceProvider.GetRequiredService<EventHubService>();
            _serviceProvider.GetRequiredService<GameTrackerService>();
            _serviceProvider.GetRequiredService<GiveawayService>();
            _serviceProvider.GetRequiredService<GuildApplicationService>();
            _serviceProvider.GetRequiredService<GamblingService>();
            _serviceProvider.GetRequiredService<VoiceChannelTrackerService>();
            _serviceProvider.GetRequiredService<RoleGiverService>();
            _serviceProvider.GetRequiredService<CommandService>();
            _serviceProvider.GetRequiredService<SignupsService>();
            await _serviceProvider.GetRequiredService<StartupService>().StartAsync();
        }

        public void Shutdown() 
        {
            _discord.LogoutAsync().GetAwaiter().GetResult();
        }
    }
}