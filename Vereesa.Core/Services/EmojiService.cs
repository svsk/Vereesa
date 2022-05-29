using System;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using Vereesa.Core.Extensions;
using Vereesa.Core.Infrastructure;

namespace Vereesa.Core.Services
{
	public class EmojiService : BotServiceBase
	{
		private readonly ILogger<EmojiService> _logger;

		private ulong _neonGuildId = 124246560178438145;

		private IRole _officerRole => Discord.GetRole(124251615489294337);

		public EmojiService(DiscordSocketClient discord, ILogger<EmojiService> logger)
			: base(discord)
		{
			_logger = logger;
		}

		[OnCommand("!emoji list")]
		public async Task ListEmojis(IMessage message)
		{
			var emotes = Discord.GetGuild(_neonGuildId).Emotes;

			await message.Channel.SendMessageAsync(string.Join(" ", emotes.Select(e => $"<:{e.Name}:{e.Id}>")));
		}

		[OnCommand("!emoji suggest")]
		[WithArgument("emojiname", 0)]
		[Description("Suggest a picture to turned into an emoji. Emoji name must be single word. And a picture must" +
			" be attached to the command message.")]
		[CommandUsage("`!emoji suggest <emoji name>`")]
		[AsyncHandler]
		public async Task SuggestEmoji(IMessage message, string emojiName)
		{
			if (message.Attachments.Count != 1)
			{
				await message.Channel
					.SendMessageAsync("Please attach an image file to your suggestion message for evalulation.");
			}

			if (string.IsNullOrWhiteSpace(emojiName))
			{
				await message.Channel.SendMessageAsync("Please include an emoji name.");
			}

			var response = await Prompt(_officerRole, "Should this be an emoji? (answer `yes` to confirm)",
				message.Channel, 60000);

			var responseMessage = "No officer responded. Please try again later.";

			if (response != null && response.Content.Equals("yes", StringComparison.InvariantCultureIgnoreCase))
			{
				try
				{
					var emoteUrl = message.Attachments.First().Url;
					var request = WebRequest.Create(emoteUrl);
					var emoteImage = new Image(request.GetResponse().GetResponseStream());
					var emote = await Discord.GetGuild(_neonGuildId).CreateEmoteAsync(emojiName, emoteImage);
					responseMessage = $"OK, I made <:{emote.Name}:{emote.Id}>!";
				}
				catch (Exception ex)
				{
					responseMessage = "Ugh... I failed making that an emoji... Sorry!";
					_logger.LogError(ex, "Failed to make emoji");
				}
			}
			else if (response != null)
			{
				responseMessage = "I'm going to take that as a no.";
			}

			await message.Channel.SendMessageAsync(responseMessage);
		}
	}

}