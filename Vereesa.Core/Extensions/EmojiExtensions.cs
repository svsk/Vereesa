using System.Linq;
using Discord.WebSocket;

namespace Vereesa.Core.Extensions;

public static class EmojiExtensions
{
	public static string GetNeonEmoji(this DiscordSocketClient discord, string emojiName)
	{
		var emoji = discord.Guilds.FirstOrDefault(g => g.Name == "Neon")?.Emotes
			.FirstOrDefault(e => e.Name.ToLower() == emojiName.ToLower());

		if (emoji == null) return null;

		return $"<:{emoji.Name}:{emoji.Id}>";
	}
}
