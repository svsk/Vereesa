using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace Vereesa.Core.Extensions
{
    public static class DiscordClientExtensions
    {
        public static async Task<IMessageChannel> GetGuildChannelByNameAsync(
            this DiscordSocketClient client,
            string guildName,
            string channelName
        )
        {
            IReadOnlyCollection<IGuild> guilds = client.Guilds;
            List<IGuild> matchingGuilds = guilds.Where(g => g.Name == guildName).ToList();

            if (matchingGuilds.Count > 1)
            {
                throw new InvalidOperationException("More than one guild with the same name found.");
            }

            if (matchingGuilds.Count == 0)
            {
                throw new InvalidOperationException("No guilds with that name found.");
            }

            IGuild guild = matchingGuilds.First();
            channelName = channelName.Replace("#", string.Empty);
            IReadOnlyCollection<IChannel> channels = await guild.GetChannelsAsync();
            List<IChannel> matchingChannels = channels.Where(c => c.Name == channelName).ToList();

            if (matchingChannels.Count > 1)
            {
                throw new InvalidOperationException("More than one channel with the same name found.");
            }

            if (matchingChannels.Count == 0)
            {
                throw new InvalidOperationException("No channels with that name found.");
            }

            IMessageChannel messageChannel = matchingChannels.First() as IMessageChannel;

            if (messageChannel == null)
            {
                throw new InvalidCastException("Could not cast channel to IMessageChannel.");
            }

            return messageChannel;
        }

        public static SocketRole GetRole(this DiscordSocketClient discord, ulong roleId)
        {
            return discord.Guilds.SelectMany(g => g.Roles).FirstOrDefault(r => r.Id == roleId);
        }

        public static string GetPreferredDisplayName(this IUser author)
        {
            if (author is IGuildUser guildUser)
            {
                return guildUser.DisplayName ?? guildUser.Username;
            }
            else
            {
                return author.Username;
            }
        }
    }
}
