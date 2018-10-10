using System.Linq;
using Discord.WebSocket;

namespace Vereesa.Core.Extensions
{
    public static class SocketGuildExtensions
    {
        public static ISocketMessageChannel GetChannelByName(this SocketGuild guild, string channelName)
        {
            ISocketMessageChannel channel = guild.Channels.FirstOrDefault(c => c.Name == channelName) as ISocketMessageChannel;
            return channel;
        }
    }
}