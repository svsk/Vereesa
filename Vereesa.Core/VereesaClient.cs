using System;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Vereesa.Core.Configuration;
using Vereesa.Core.Services;
using Vereesa.Core.Integrations;
using Vereesa.Core.Integrations.Interfaces;
using Vereesa.Data.Models.Commands;
using Vereesa.Data.Models.Gambling;
using Vereesa.Data.Models.GameTracking;
using Vereesa.Data.Models.Giveaways;
using Vereesa.Data.Repositories;
using Vereesa.Data.Interfaces;
using Vereesa.Data.Models.Reminders;
using Vereesa.Data.Configuration;
using Vereesa.Data.Models.Statistics;
using Vereesa.Core.Infrastructure;
using System.Linq;

namespace Vereesa.Core
{
    public class VereesaClient
    {
        private IConfigurationRoot _config;
        private IServiceProvider _serviceProvider;
        private DiscordSocketClient _discord;

        public async Task StartupAsync()
        {
            //Set up configuration
            var builder = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("config.json", optional: false, reloadOnChange: true)
                .AddJsonFile("config.Local.json", optional: true, reloadOnChange: true);

            _config = builder.Build();
            
            var discordSettings = new DiscordSettings();
            var channelRuleSettings = new ChannelRuleSettings();
            var battleNetApiSettings = new BattleNetApiSettings();
            var gameStateEmissionSettings = new GameStateEmissionSettings();
            var gamblingSettings = new GamblingSettings();
            var voiceChannelTrackerSettings = new VoiceChannelTrackerSettings();
            var guildApplicationSettings = new GuildApplicationSettings();
            var signupsSettings = new SignupsSettings();
            var twitterClientSettings = new TwitterClientSettings();
            var twitterServiceSettings = new TwitterServiceSettings();
            var storageSettings = new AzureStorageSettings();
            var announcementServiceSettings = new AnnouncementServiceSettings();

            _config.GetSection(nameof(DiscordSettings)).Bind(discordSettings);
            _config.GetSection(nameof(ChannelRuleSettings)).Bind(channelRuleSettings);
            _config.GetSection(nameof(BattleNetApiSettings)).Bind(battleNetApiSettings);
            _config.GetSection(nameof(GameStateEmissionSettings)).Bind(gameStateEmissionSettings);
            _config.GetSection(nameof(GamblingSettings)).Bind(gamblingSettings);
            _config.GetSection(nameof(VoiceChannelTrackerSettings)).Bind(voiceChannelTrackerSettings);
            _config.GetSection(nameof(GuildApplicationSettings)).Bind(guildApplicationSettings);
            _config.GetSection(nameof(SignupsSettings)).Bind(signupsSettings);
            _config.GetSection(nameof(TwitterClientSettings)).Bind(twitterClientSettings);
            _config.GetSection(nameof(TwitterServiceSettings)).Bind(twitterServiceSettings);
            _config.GetSection(nameof(AzureStorageSettings)).Bind(storageSettings);
            _config.GetSection(nameof(AnnouncementServiceSettings)).Bind(announcementServiceSettings);

            //Set up discord client
            _discord = new DiscordSocketClient(new DiscordSocketConfig
            {
                LogLevel = LogSeverity.Verbose,
                MessageCacheSize = 1000
            });

            //Set up a service provider with all relevant resources for DI
            IServiceCollection services = new ServiceCollection()
                .AddSingleton<DiscordSocketClient>(_discord)
                .AddSingleton<IDiscordSocketClient>(new InterfacedDiscordSocketClient(_discord))
                .AddSingleton(discordSettings)
                .AddSingleton(channelRuleSettings)
                .AddSingleton(battleNetApiSettings)
                .AddSingleton(gameStateEmissionSettings)
                .AddSingleton(gamblingSettings)
                .AddSingleton(voiceChannelTrackerSettings)
                .AddSingleton(guildApplicationSettings)
                .AddSingleton(signupsSettings)
                .AddSingleton(twitterClientSettings)
                .AddSingleton(twitterServiceSettings)
                .AddSingleton(announcementServiceSettings)
                .AddSingleton(storageSettings)
                .AddSingleton<ChannelRuleService>()
                .AddSingleton<Random>()
                .AddSingleton<StartupService>()
                .AddSingleton<EventHubService>()
                .AddSingleton<NeonApiService>()
                .AddSingleton<PingService>()
                .AddSingleton<BattleNetApiService>()
                .AddSingleton<GameTrackerService>()
                .AddSingleton<GiveawayService>()
                .AddSingleton<GuildApplicationService>()
                .AddSingleton<GamblingService>()
                .AddSingleton<VoiceChannelTrackerService>()
                .AddSingleton<RoleGiverService>()    
                .AddSingleton<CommandService>()
                .AddSingleton<SignupsService>()
                .AddSingleton<TodayInWoWService>()
                .AddSingleton<MovieSuggestionService>()
                .AddSingleton<TwitterService>()
                .AddSingleton<RemindMeService>()
                .AddSingleton<AnnouncementService>()
                .AddSingleton<CoronaService>()
                .AddSingleton<AuctionHouseService>()
                .AddSingleton<FlagService>()
                .AddSingleton<InvasionService>()
                .AddScoped<IRepository<GameTrackMember>, AzureStorageRepository<GameTrackMember>>()
                .AddScoped<IRepository<Giveaway>, AzureStorageRepository<Giveaway>>()
                .AddScoped<IRepository<GamblingStandings>, AzureStorageRepository<GamblingStandings>>()
                .AddScoped<IRepository<Reminder>, AzureStorageRepository<Reminder>>()
                .AddScoped<IRepository<Command>, AzureStorageRepository<Command>>()
                .AddScoped<IRepository<Statistics>, AzureStorageRepository<Statistics>>()
                .AddScoped<IWowheadClient, WowheadClient>()
                .AddTransient<TwitterClient>()
                .AddLogging(config => { 
                    config.AddConsole();
                    config.AddProvider(new DiscordChannelLoggerProvider(_discord, 124446036637908995, LogLevel.Error)); // todo: config the channel id?
                });

            //Build the service provider
            _serviceProvider = services.BuildServiceProvider();

            //Start the desired services
            try 
            {
                foreach (var s in services.Where(s => s.ServiceType.BaseType == typeof(BotServiceBase))) 
                {
                    _serviceProvider.GetRequiredService(s.ServiceType);
                }

                _serviceProvider.GetRequiredService<AnnouncementService>(); // todo: make this a BotService
            }
            catch (Exception) 
            {
            }
            
            await _serviceProvider.GetRequiredService<StartupService>().StartAsync();
        }

        public void Shutdown() 
        {
            _discord.LogoutAsync().GetAwaiter().GetResult();
        }
    }
}