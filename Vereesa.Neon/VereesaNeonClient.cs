using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Vereesa.Core;
using Vereesa.Core.Discord;
using Vereesa.Neon.Configuration;
using Vereesa.Neon.Data.Configuration;
using Vereesa.Neon.Data.Interfaces;
using Vereesa.Neon.Data.Models.Attendance;
using Vereesa.Neon.Data.Models.Commands;
using Vereesa.Neon.Data.Models.Gambling;
using Vereesa.Neon.Data.Models.GameTracking;
using Vereesa.Neon.Data.Models.Giveaways;
using Vereesa.Neon.Data.Models.Reminders;
using Vereesa.Neon.Data.Models.Statistics;
using Vereesa.Neon.Data.Repositories;
using Vereesa.Neon.Helpers;
using Vereesa.Neon.Integrations;
using Vereesa.Neon.Integrations.Interfaces;
using Vereesa.Neon.Services;

namespace Vereesa.Neon
{
    public class VereesaNeonClient
    {
        private VereesaHost? _host;
        private IConfigurationRoot? _config;
        private readonly ulong _logChannelId = 124446036637908995;

        public void Start(Action<IServiceCollection, IConfigurationRoot> config)
        {
            //Set up configuration
            var builder = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("config.json", optional: false, reloadOnChange: true)
                .AddJsonFile("config.Local.json", optional: true, reloadOnChange: true);

            _config = builder.Build();

            var channelRuleSettings = new ChannelRuleSettings();
            var battleNetApiSettings = new BattleNetApiSettings();
            var gameStateEmissionSettings = new GameStateEmissionSettings();
            var gamblingSettings = new GamblingSettings();
            var voiceChannelTrackerSettings = new VoiceChannelTrackerSettings();
            var guildApplicationSettings = new GuildApplicationSettings();
            var signupsSettings = new SignupsSettings();
            var storageSettings = new AzureStorageSettings();
            var warcraftLogsApiSettings = new WarcraftLogsApiSettings();
            var openAISettings = new OpenAISettings();

            var discordSettings = new DiscordSettings();
            _config.Bind(discordSettings);

            _config.GetSection(nameof(DiscordSettings)).Bind(discordSettings);
            _config.GetSection(nameof(ChannelRuleSettings)).Bind(channelRuleSettings);
            _config.GetSection(nameof(BattleNetApiSettings)).Bind(battleNetApiSettings);
            _config.GetSection(nameof(GameStateEmissionSettings)).Bind(gameStateEmissionSettings);
            _config.GetSection(nameof(GamblingSettings)).Bind(gamblingSettings);
            _config.GetSection(nameof(VoiceChannelTrackerSettings)).Bind(voiceChannelTrackerSettings);
            _config.GetSection(nameof(GuildApplicationSettings)).Bind(guildApplicationSettings);
            _config.GetSection(nameof(SignupsSettings)).Bind(signupsSettings);
            _config.GetSection(nameof(AzureStorageSettings)).Bind(storageSettings);
            _config.GetSection(nameof(WarcraftLogsApiSettings)).Bind(warcraftLogsApiSettings);
            _config.GetSection(nameof(OpenAISettings)).Bind(openAISettings);

            var httpClient = new HttpClient();

            //Set up a service provider with all relevant resources for DI
            var vereesaHost = new VereesaHostBuilder()
                .AddDiscord(discordSettings.Token)
                .AddDiscordChannelLogging(_logChannelId, LogLevel.Warning)
                .AddServices(services =>
                {
                    services
                        .AddSingleton(channelRuleSettings)
                        .AddSingleton(battleNetApiSettings)
                        .AddSingleton(gameStateEmissionSettings)
                        .AddSingleton(gamblingSettings)
                        .AddSingleton(voiceChannelTrackerSettings)
                        .AddSingleton(guildApplicationSettings)
                        .AddSingleton(signupsSettings)
                        .AddSingleton(storageSettings)
                        .AddSingleton(warcraftLogsApiSettings)
                        .AddSingleton(openAISettings)
                        .AddSingleton(httpClient)
                        .AddSingleton<Random>()
                        .AddTransient<AttendanceService>()
                        .AddTransient<AuctionHouseService>()
                        .AddTransient<BattleNetApiService>()
                        .AddTransient<FlagService>()
                        .AddSingleton<NeonCoinService>()
                        .AddTransient<NeonConKeyRetrieverService>()
                        .AddTimeZoneService()
                        .AddTodayInWoWService()
                        .AddScoped<IWarcraftLogsApi, WarcraftLogsApi>()
                        .AddScoped<IWarcraftLogsScraper, WarcraftLogsScraper>()
                        .AddScoped<ISpreadsheetClient, GoogleSheetsClient>()
                        .AddSingleton<ISimpleStore>(new SimpleStore(WellknownPaths.AppData))
                        .AddScoped<IRepository<GameTrackMember>, AzureStorageRepository<GameTrackMember>>()
                        .AddScoped<IRepository<Giveaway>, AzureStorageRepository<Giveaway>>()
                        .AddScoped<IRepository<GamblingStandings>, AzureStorageRepository<GamblingStandings>>()
                        .AddScoped<IRepository<Reminder>, AzureStorageRepository<Reminder>>()
                        .AddScoped<IRepository<Command>, AzureStorageRepository<Command>>()
                        .AddScoped<IRepository<Statistics>, AzureStorageRepository<Statistics>>()
                        .AddScoped<IRepository<RaidAttendance>, AzureStorageRepository<RaidAttendance>>()
                        .AddScoped<IRepository<RaidAttendanceSummary>, AzureStorageRepository<RaidAttendanceSummary>>()
                        .AddScoped<IRepository<UsersCharacters>, AzureStorageRepository<UsersCharacters>>()
                        .AddScoped<IRepository<Personality>, AzureStorageRepository<Personality>>()
                        .AddLogging(config =>
                        {
                            config.AddConsole();
                        });

                    config?.Invoke(services, _config);
                });

            _host = vereesaHost.Start();

            // 			_serviceProvider.GetRequiredService<ILogger<VereesaClient>>().LogWarning(@"`
            // 							Neon's own Discord Bot!
            // ██╗   ██╗███████╗██████╗ ███████╗███████╗███████╗ █████╗
            // ██║   ██║██╔════╝██╔══██╗██╔════╝██╔════╝██╔════╝██╔══██╗
            // ██║   ██║█████╗  ██████╔╝█████╗  █████╗  ███████╗███████║
            // ╚██╗ ██╔╝██╔══╝  ██╔══██╗██╔══╝  ██╔══╝  ╚════██║██╔══██║
            //  ╚████╔╝ ███████╗██║  ██║███████╗███████╗███████║██║  ██║
            //   ╚═══╝  ╚══════╝╚═╝  ╚═╝╚══════╝╚══════╝╚══════╝╚═╝  ╚═╝
            // 	I am now accepting your requests!
            // `");
        }

        public async Task Shutdown()
        {
            if (_host != null)
            {
                await _host.Shutdown();
            }
        }
    }
}
