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
            _discord.GuildAvailable += OnGuildAvailable;
            _discord.GuildMemberUpdated += OnMemberUpdated;
        }

        private async Task OnGuildAvailable(SocketGuild arg)
        {
            //Do initial scan

        }

        private async Task OnMemberUpdated(SocketGuildUser userBeforeChange, SocketGuildUser userAfterChange)
        {
            var beforeGame = userBeforeChange.Game;
            var afterGame = userAfterChange.Game;

            if (beforeGame != null && (afterGame == null || afterGame.Value.Name != beforeGame.Value.Name))
            {
                StoppedPlaying(userBeforeChange);
            }
            
            if (afterGame != null)
            {
                StartedPlaying(userAfterChange);
            }
        }

        private void StartedPlaying(SocketGuildUser user)
        {
            System.IO.File.AppendAllText(@"C:\temp\gamelog.json", $"{DateTime.UtcNow} | {user.Username} started playing {user.Game.Value.Name}.{Environment.NewLine}");
        }

        private void StoppedPlaying(SocketGuildUser user)
        {
            System.IO.File.AppendAllText(@"C:\temp\gamelog.json", $"{DateTime.UtcNow} | {user.Username} stopped playing {user.Game.Value.Name}.{Environment.NewLine}");
        }
    }
}