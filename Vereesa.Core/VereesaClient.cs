using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Vereesa.Core.Configuration;
using Vereesa.Core.Extensions;
using Vereesa.Core.Infrastructure;
using Vereesa.Core.Integrations;
using Vereesa.Core.Integrations.Interfaces;
using Vereesa.Core.Services;
using Vereesa.Data.Configuration;
using Vereesa.Data.Interfaces;
using Vereesa.Data.Models.Commands;
using Vereesa.Data.Models.Gambling;
using Vereesa.Data.Models.GameTracking;
using Vereesa.Data.Models.Giveaways;
using Vereesa.Data.Models.Reminders;
using Vereesa.Data.Models.Statistics;
using Vereesa.Data.Repositories;

namespace Vereesa.Core
{
	public class VereesaClient
	{
		private IConfigurationRoot _config;
		private IServiceProvider _serviceProvider;
		private DiscordSocketClient _discord;

		public async Task StartupAsync(Action<IServiceCollection, IConfigurationRoot> config = null)
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
			var twitterServiceSettings = new TwitterServiceSettings();
			var storageSettings = new AzureStorageSettings();
			var warcraftLogsApiSettings = new WarcraftLogsApiSettings();
			var openAISettings = new OpenAISettings();

			_config.Bind(discordSettings);

			_config.GetSection(nameof(DiscordSettings)).Bind(discordSettings);
			_config.GetSection(nameof(ChannelRuleSettings)).Bind(channelRuleSettings);
			_config.GetSection(nameof(BattleNetApiSettings)).Bind(battleNetApiSettings);
			_config.GetSection(nameof(GameStateEmissionSettings)).Bind(gameStateEmissionSettings);
			_config.GetSection(nameof(GamblingSettings)).Bind(gamblingSettings);
			_config.GetSection(nameof(VoiceChannelTrackerSettings)).Bind(voiceChannelTrackerSettings);
			_config.GetSection(nameof(GuildApplicationSettings)).Bind(guildApplicationSettings);
			_config.GetSection(nameof(SignupsSettings)).Bind(signupsSettings);
			_config.GetSection(nameof(TwitterServiceSettings)).Bind(twitterServiceSettings);
			_config.GetSection(nameof(AzureStorageSettings)).Bind(storageSettings);
			_config.GetSection(nameof(WarcraftLogsApiSettings)).Bind(warcraftLogsApiSettings);
			_config.GetSection(nameof(OpenAISettings)).Bind(openAISettings);

			//Set up discord client
			_discord = new DiscordSocketClient(new DiscordSocketConfig
			{
				LogLevel = LogSeverity.Verbose,
				MessageCacheSize = 1000,
				AlwaysDownloadUsers = true,
				GatewayIntents = GatewayIntents.All
			});

			//Set up a service provider with all relevant resources for DI
			IServiceCollection services = new ServiceCollection()
				.AddSingleton<DiscordSocketClient>(_discord)
				.AddSingleton(discordSettings)
				.AddSingleton(channelRuleSettings)
				.AddSingleton(battleNetApiSettings)
				.AddSingleton(gameStateEmissionSettings)
				.AddSingleton(gamblingSettings)
				.AddSingleton(voiceChannelTrackerSettings)
				.AddSingleton(guildApplicationSettings)
				.AddSingleton(signupsSettings)
				.AddSingleton(twitterServiceSettings)
				.AddSingleton(storageSettings)
				.AddSingleton(warcraftLogsApiSettings)
				.AddSingleton(openAISettings)
				.AddSingleton<Random>()
				.AddSingleton<IJobScheduler, JobScheduler>()
				.AddBotServices()
				.AddScoped<IWarcraftLogsApi, WarcraftLogsApi>()
				.AddScoped<ISpreadsheetClient, GoogleSheetsClient>()
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
				.AddScoped<IWowheadClient, WowheadClient>()
				.AddLogging(config =>
				{
					config.AddConsole();
					config.AddProvider(new DiscordChannelLoggerProvider(_discord, 124446036637908995, LogLevel.Warning)); // todo: config the channel id?
				});

			config?.Invoke(services, _config);

			//Build the service provider
			_serviceProvider = services.BuildServiceProvider();


			//Start the desired services
			try
			{
				foreach (var s in services.Where(s => s.ServiceType.BaseType == typeof(BotServiceBase)))
				{
					_serviceProvider.GetRequiredService(s.ServiceType);
				}
			}
			catch (Exception)
			{
			}

			await _serviceProvider.GetRequiredService<StartupService>().StartAsync();

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

		public void Shutdown()
		{
			_discord.LogoutAsync().GetAwaiter().GetResult();
		}
	}
}
