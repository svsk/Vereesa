using System;
using System.Collections.Generic;
using System.Linq;
using Discord;

namespace Vereesa.Core.Extensions
{
    public static class IDiscordClientExtensions
    {
        public static IMessageChannel GetGuildChannelByName(this IDiscordClient client, string guildName, string channelName) 
        {
            IReadOnlyCollection<IGuild> guilds = client.GetGuildsAsync().GetAwaiter().GetResult();
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
            IReadOnlyCollection<IChannel> channels = guild.GetChannelsAsync().GetAwaiter().GetResult();
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
    }
}