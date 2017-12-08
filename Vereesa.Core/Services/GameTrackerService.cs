using System;
using System.Linq;
using System.Threading.Tasks;
using Discord.WebSocket;

namespace Vereesa.Core.Services
{
    public class GameTrackerService
    {
        private DiscordSocketClient _discord;

        public GameTrackerService(DiscordSocketClient discord)
        {
            _discord = discord;
            _discord.GuildMemberUpdated += OnMemberUpdated;
        }

        private async Task OnMemberUpdated(SocketGuildUser before, SocketGuildUser after)
        {
            if (after.Game != null) {
                //User has started playing game
            } else if (before.Game != null && after.Game == null) {
                //User is no longer playing game
            }
        }
    }
}