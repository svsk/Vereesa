using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using Vereesa.Core.Configuration;
using Vereesa.Core.Extensions;
using Vereesa.Core.Helpers;
using Vereesa.Data.Models.BattleNet;
using Vereesa.Data.Models.NeonApi;
using Vereesa.Core.Infrastructure;

namespace Vereesa.Core.Services
{
	public class GuildApplicationService : BotServiceBase
	{
		private NeonApiService _neonApiService;
		private ILogger<GuildApplicationService> _logger;
		private DiscordSocketClient _discord;
		private BattleNetApiService _battleNetApi;
		private GuildApplicationSettings _settings;
		private Dictionary<string, ApplicationListItem> _cachedApplications;

		//Dictionary where the key is an application's ID and the value is a list containing numbers representing hour 
		//thresholds for when reminders were sent to nofity officers of an overdue application.
		private Dictionary<string, List<int>> _notificationCache;

		/// This service is fully async
		public GuildApplicationService(NeonApiService neonApiService, DiscordSocketClient discord, GuildApplicationSettings settings, BattleNetApiService battleNetApi, ILogger<GuildApplicationService> logger)
			: base(discord)
		{
			_logger = logger;
			_discord = discord;
			_battleNetApi = battleNetApi;
			_settings = settings;
			_neonApiService = neonApiService;

			_discord.Ready -= Start;
			_discord.Ready += Start;
		}

		private async Task Start()
		{
			await TimerHelpers.SetTimeoutAsync(async () =>
			{
				var sw = new Stopwatch();
				sw.Start();
				_logger.LogInformation("Getting applications.");
				var applications = await _neonApiService.GetApplicationListAsync();

				if (applications != null)
				{
					_logger.LogInformation($"Getting {applications.Count()} applications took {sw.ElapsedMilliseconds} ms.");
					await CheckForNewApplications(applications);
					await CheckForOverdueApplications(applications);
				}
			}, 60000 * 2, true, true);

			_discord.MessageReceived -= HandleMessage;
			_discord.MessageReceived += HandleMessage;
		}

		private async Task HandleMessage(SocketMessage message)
		{
			if (message.Content == "!debug applications")
			{
				try
				{
					var applicationEmbed = (await GetApplicationEmbedAsync(_cachedApplications.Last().Key)).Build();
					await message.Channel.SendMessageAsync(null, embed: applicationEmbed);
				}
				catch (Exception ex)
				{
					await message.Channel.SendMessageAsync(ex.Message + " " + ex.StackTrace);
				}
			}
		}

		private async Task CheckForOverdueApplications(IEnumerable<ApplicationListItem> applications)
		{
			var isFirstRun = _notificationCache == null;
			_notificationCache = isFirstRun ? new Dictionary<string, List<int>>() : _notificationCache;
			var notificationHourThresholds = new int[] { 36, 48 }; //Config?

			foreach (var application in applications)
			{
				if (application.Status == "Pending")
				{
					_notificationCache[application.Id] = _notificationCache.ContainsKey(application.Id) ? _notificationCache[application.Id] : new List<int>();

					var timeSinceAppSubmission = DateTime.UtcNow - application.Timestamp.ToUtc(_settings.SourceTimeZone);
					var hoursPending = (int)Math.Floor(timeSinceAppSubmission.TotalHours);
					var shouldNotify = false;
					var previousNotificationTimes = _notificationCache[application.Id];

					foreach (var threshold in notificationHourThresholds)
					{
						if (hoursPending >= threshold && !previousNotificationTimes.Contains(threshold))
						{
							shouldNotify = true;
							previousNotificationTimes.Add(threshold);
						}
					}

					if (shouldNotify && !isFirstRun)
					{
						await SendOverdueNotification(application.Id, hoursPending);
					}
				}
			}
		}

		private async Task SendOverdueNotification(string applicationId, int hoursPending)
		{
			_logger.LogInformation($"Application {applicationId} has now been pending for {hoursPending} hours.");

			Application application = await _neonApiService.GetApplicationByIdAsync(applicationId);

			var playerName = application.GetFirstAnswerByEitherQuestionPart("your name", "full name");

			var notificationChannel = GetNotificationChannel();

			await notificationChannel?.SendMessageAsync($"<@&{_settings.NotificationRoleId}> {playerName}'s application has now been pending for more than {hoursPending} hours. Please review it and respond as soon as possible.");
		}

		private async Task CheckForNewApplications(IEnumerable<ApplicationListItem> applications)
		{
			var isFirstRun = _cachedApplications == null;
			if (isFirstRun)
			{
				_cachedApplications = new Dictionary<string, ApplicationListItem>();
			}

			foreach (var application in applications)
			{
				if (!_cachedApplications.ContainsKey(application.Id) && !isFirstRun)
				{
					_logger.LogInformation($"Found new app! Announcing!");
					await AnnounceNewApplication(application.Id);
				}

				if (_cachedApplications.ContainsKey(application.Id))
				{
					ApplicationListItem cachedApplication = _cachedApplications[application.Id];

					if (cachedApplication.Status != application.Status)
					{
						//status changed
						var appMessage = await RetrieveApplicationMessageAsync(application.Id);
						if (appMessage != null)
						{
							await appMessage.ModifyAsync(async (msg) =>
							{
								msg.Embed = (await GetApplicationEmbedAsync(application.Id)).Build();
							});
						}
					}
				}

				_cachedApplications[application.Id] = application;
			}
		}

		private async Task AnnounceNewApplication(string applicationId)
		{
			var notificationChannel = GetNotificationChannel();
			if (notificationChannel != null)
			{
				try
				{
					//await notificationChannel.SendMessageAsync(string.Format(_settings.MessageToSendOnNewLine, fields));
					var applicationEmbed = (await GetApplicationEmbedAsync(applicationId)).Build();
					await notificationChannel.SendMessageAsync(string.Empty, false, applicationEmbed);
				}
				catch (Exception ex)
				{
					_logger.LogError(ex.Message, ex);
				}
			}
		}

		private ISocketMessageChannel GetNotificationChannel()
		{
			return _discord.Guilds.FirstOrDefault()?.Channels.FirstOrDefault(c => c.Name == _settings.NotificationMessageChannelName) as ISocketMessageChannel;
		}

		private async Task<IUserMessage> RetrieveApplicationMessageAsync(string applicationId)
		{
			ISocketMessageChannel channel = GetNotificationChannel();
			IAsyncEnumerable<IMessage> messages = channel.GetMessagesAsync(100).Flatten();
			IUserMessage applicationEmbedMessage = null;

			await messages.ForEachAsync((msg) =>
			{
				var embed = msg.Embeds.FirstOrDefault();
				if (embed != null && embed.Author != null && embed.Author.Value.Url.Contains($"id={applicationId}"))
				{
					applicationEmbedMessage = msg as IUserMessage;
				}
			});

			return applicationEmbedMessage;
		}

		private async Task<EmbedBuilder> GetApplicationEmbedAsync(string applicationId)
		{
			var application = await _neonApiService.GetApplicationByIdAsync(applicationId);

			var armoryProfileUrl = application.GetFirstAnswerByQuestionPart("wow-armory profile");
			var charAndRealm = ParseArmoryLink(armoryProfileUrl);
			var character = (BattleNetCharacterResponse)_battleNetApi.GetCharacterData(charAndRealm.realm, charAndRealm.name, "eu"); // Why is this type not inferred implicitly?
			var avatar = _battleNetApi.GetCharacterThumbnail("eu", charAndRealm.realm, charAndRealm.name);
			var artifactLevel = _battleNetApi.GetCharacterHeartOfAzerothLevel(character);

			var characterName = application.GetFirstAnswerByQuestionPart("main character");
			var characterSpec = application.GetFirstAnswerByQuestionPart("role/spec");
			var characterClass = application.GetFirstAnswerByQuestionPart("what class");
			var playerName = application.GetFirstAnswerByEitherQuestionPart("your name", "full name");
			var playerAge = application.GetFirstAnswerByQuestionPart("how old are you");
			var playerCountry = application.GetFirstAnswerByQuestionPart("where are you from");

			var embed = new EmbedBuilder();
			embed.Color = new Color(155, 89, 182);
			embed.WithAuthor($"New application @ neon.gg/applications", null, $"https://www.neon.gg/applications/?id={application.Id}");
			embed.WithThumbnailUrl(avatar);

			var title = $"{characterName} - {characterSpec} {characterClass}";
			embed.Title = title.Length > 256 ? title.Substring(0, 256) : title;
			embed.AddField("__Real Name__", playerName, true);
			embed.AddField("__Age__", playerAge, true);
			embed.AddField("__Country__", playerCountry, true);
			embed.AddField("__Status__", GetIconedStatusString(application.CurrentStatusString), true);
			embed.AddField("__Character Stats__", $"**Heart of Azeroth level:** {artifactLevel} \r\n**Avg ilvl:** {/*character?.ItemLevel*/ 0}\r\n**Achi points:** {/*character?.AchievementPoints*/ 0} | **Total HKs:** {/* character?.TotalHonorableKills*/ 0}", false);
			embed.AddField("__External sites__", $@"[Armory]({armoryProfileUrl}) | [RaiderIO](https://raider.io/characters/eu/{charAndRealm.realm}/{charAndRealm.name}) | [WoWProgress](https://www.wowprogress.com/character/eu/{charAndRealm.realm}/{charAndRealm.name}) | [WarcraftLogs](https://www.warcraftlogs.com/character/eu/{charAndRealm.realm}/{charAndRealm.name})", false);

			embed.Footer = new EmbedFooterBuilder();
			embed.Footer.WithIconUrl("https://render-eu.worldofwarcraft.com/character/karazhan/102/54145126-avatar.jpg");
			embed.Footer.Text = $"Requested by Veinlash - Today at {DateTimeExtensions.NowInCentralEuropeanTime().ToString("HH:mm")}";

			return embed;
		}

		private string GetIconedStatusString(string currentStatusString)
		{
			switch (currentStatusString)
			{
				case "Pending":
					return "⚠️ Pending";
				case "Declined":
					return "🛑 Declined";
				case "Accepted":
					return "✅ Accepted";
				default:
					return currentStatusString;
			}
		}

		//handles:
		//https://raider.io/characters/eu/the-maelstrom/Connon
		//https://worldofwarcraft.com/en-gb/character/the-maelstrom/Boop
		//https://www.warcraftlogs.com/character/eu/karazhan/veinlash
		private dynamic ParseArmoryLink(string armoryLink)
		{
			var realmAndNameSplit = armoryLink.Split('?').First().Trim('/').Split('/').Reverse();
			var name = realmAndNameSplit.Skip(0).Take(1).First();
			var realm = realmAndNameSplit.Skip(1).Take(1).First();

			return new
			{
				realm = realm,
				name = name
			};
		}
	}
}