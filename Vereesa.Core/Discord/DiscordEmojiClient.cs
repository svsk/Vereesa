using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Vereesa.Core.Models;

namespace Vereesa.Core.Discord
{
    public class DiscordEmojiClient : IEmojiClient
    {
        public DiscordSocketClient Discord { get; }

        public DiscordEmojiClient(DiscordSocketClient discord)
        {
            Discord = discord;
        }

        public string GetCustomEmoji(string emojiName)
        {
            // Make this dynamic?
            var guildName = "Neon";

            var emoji = Discord.Guilds
                .FirstOrDefault(g => g.Name == guildName)
                ?.Emotes.FirstOrDefault(e => e.Name.ToLower() == emojiName.ToLower());

            if (emoji == null)
                return null;

            return $"<:{emoji.Name}:{emoji.Id}>";
        }

        public IReadOnlyCollection<VereesaEmoji> GetCustomEmojiByServerId(ulong guildId)
        {
            var emotes = Discord.GetGuild(guildId).Emotes;
            return emotes.Select(e => new VereesaEmoji { Id = e.Id, Name = e.Name }).ToList();
        }

        public async Task<VereesaEmoji> CreateCustomEmoji(ulong guildId, string emojiName, Image emoteImage)
        {
            var emote = await Discord.GetGuild(guildId).CreateEmoteAsync(emojiName, emoteImage);
            return new VereesaEmoji { Id = emote.Id, Name = emote.Name };
        }
    }
}
