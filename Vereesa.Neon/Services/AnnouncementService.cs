// using System;
// using System.Linq;
// using System.Threading.Tasks;
// using Discord.WebSocket;
// using Microsoft.Extensions.Logging;
// using Vereesa.Neon.Configuration;
// using Vereesa.Core.Extensions;
// using Vereesa.Neon.Integrations;

// namespace Vereesa.Neon.Services
// {
// 	[Obsolete]
// 	public class AnnouncementService : TwitterService
// 	{
// 		private DiscordSocketClient _discord;
// 		private AnnouncementServiceSettings _settings;

// 		public AnnouncementService(
// 			TwitterClient twitter,
// 			DiscordSocketClient discord,
// 			AnnouncementServiceSettings settings,
// 			ILogger<TwitterService> baseLogger
// 		) : base(settings, twitter, discord, baseLogger)
// 		{
// 			_discord = discord;
// 			_settings = settings;
// 		}

// 		protected override async Task SendTweetToTargetChannelAsync(Tweet tweet)
// 		{
// 			var guilds = _discord.Guilds;

// 			var targetGuild = guilds.FirstOrDefault(g => g.Name == _settings.TargetDiscordGuild);


// 			var announcementText = tweet.FullText;

// 			var mentions = announcementText.Split(" ")
// 				.Where(word => word.StartsWith("@") && word.Length > 1)
// 				.Select(word => word.Substring(1))
// 				.Distinct()
// 				.ToList();

// 			if (mentions.Any())
// 			{
// 				var guildUsers = targetGuild.Users;

// 				foreach (var mention in mentions)
// 				{
// 					var mentionedEveryone = mention.ToLowerInvariant() == "everyone";
// 					var mentionedRole = targetGuild.Roles.FirstOrDefault(role => role.Name.ToLowerInvariant() == mention.ToLowerInvariant());
// 					var mentionedUser = guildUsers.FirstOrDefault(user => user.Username.ToLowerInvariant() == mention.ToLowerInvariant());

// 					if (mentionedRole != null)
// 					{
// 						announcementText = announcementText.Replace($"@{mention}", $"{mentionedRole.Mention}");
// 					}
// 					else if (mentionedUser != null)
// 					{
// 						announcementText = announcementText.Replace($"@{mention}", $"{mentionedUser.Mention}");
// 					}
// 					else if (mentionedEveryone)
// 					{
// 						announcementText = announcementText.Replace($"@{mention}", $"{targetGuild.EveryoneRole.Mention}");
// 					}
// 				}
// 			}

// 			var targetChannel = await _discord.GetGuildChannelByNameAsync(_settings.TargetDiscordGuild, _settings.TargetDiscordChannel);
// 			await targetChannel.SendMessageAsync(announcementText);
// 		}
// 	}
// }
