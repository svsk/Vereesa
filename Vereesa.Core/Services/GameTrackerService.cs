using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Vereesa.Data;
using Vereesa.Data.Models.GameTracking;

namespace Vereesa.Core.Services
{
    public class GameTrackerService
    {
        private DiscordSocketClient _discord;
        private JsonRepository<GameTrackMember> _trackingRepo;

        public GameTrackerService(DiscordSocketClient discord, JsonRepository<GameTrackMember> trackingRepo)
        {
            _discord = discord;
            _trackingRepo = trackingRepo;
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
                UpdateUserGameState(userBeforeChange, "stopped");
            }
            
            if (afterGame != null)
            {
                UpdateUserGameState(userAfterChange, "started");
            }
        }

        private void UpdateUserGameState(SocketGuildUser user, string eventType)
        {
            var userHistory = GetGameTrackMember(user);
            var gameName = user.Game.Value.Name;
            var gameHistory = userHistory.GetGameHistory(gameName);
            gameHistory.Add(GameTrackEntry.CreateInstance(eventType));
            _trackingRepo.Save();
        }

        private GameTrackMember GetGameTrackMember(SocketGuildUser user) 
        {
            var member = _trackingRepo.GetAll().FirstOrDefault(m => m.Id == user.Id.ToString());
            if (member == null) 
            {
                member = new GameTrackMember();
                member.Id = user.Id.ToString();
                member.Username = user.Username;
                member.GameHistory = new Dictionary<string, ICollection<GameTrackEntry>>();
                _trackingRepo.Add(member);
            }

            return member;
        }
    }
}