using System.Linq;
using Discord.WebSocket;

namespace Vereesa.Core.Extensions
{
    public static class SocketGuildExtensions
    {
        public static ISocketMessageChannel GetChannelByName(this SocketGuild guild, string channelName)
        {
            return guild.Channels.FirstOrDefault(c => c.Name == channelName) as ISocketMessageChannel;
        }
    }
}