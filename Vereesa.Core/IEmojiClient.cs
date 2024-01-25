using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Vereesa.Core.Models;

namespace Vereesa.Core;

public interface IEmojiClient
{
    string GetCustomEmoji(string emojiName);
    IReadOnlyCollection<VereesaEmoji> GetCustomEmojiByServerId(ulong neonGuildId);
    Task<VereesaEmoji> CreateCustomEmoji(ulong guildId, string emojiName, Image emoteImage);
}
