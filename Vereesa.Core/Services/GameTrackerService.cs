using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Vereesa.Data;
using Vereesa.Data.Models.GameTracking;

namespace Vereesa.Core.Services
{
    public class GameTrackerService
    {
        private DiscordSocketClient _discord;
        private JsonRepository<GameTrackMember> _trackingRepo;
        private IConfigurationRoot _options;

        public GameTrackerService(DiscordSocketClient discord, JsonRepository<GameTrackMember> trackingRepo, IConfigurationRoot options)
        {
            _discord = discord;
            _trackingRepo = trackingRepo;
            _discord.GuildAvailable += OnGuildAvailable;
            _discord.GuildMemberUpdated += OnMemberUpdated;
            _options = options;
        }

        private async Task OnGuildAvailable(SocketGuild guild)
        {
            await EmitGameState(guild);
        }

        private async Task OnMemberUpdated(SocketGuildUser userBeforeChange, SocketGuildUser userAfterChange)
        {
            var gameChangeHappened = false;
            var beforeGame = userBeforeChange.Game;
            var afterGame = userAfterChange.Game;

            if (beforeGame != null && (afterGame == null || afterGame.Value.Name != beforeGame.Value.Name))
            {
                UpdateUserGameState(userBeforeChange, "stopped");
                gameChangeHappened = true;
            }
            
            if (afterGame != null)
            {
                UpdateUserGameState(userAfterChange, "started");
                gameChangeHappened = true;
            }

            if (gameChangeHappened) 
            {
                await EmitGameState(userAfterChange.Guild);
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

        private  async Task EmitGameState(SocketGuild guild) 
        {            
            var gameNames = guild.Users.Where(u => u.Game != null && u.IsBot == false)
                .Select(u => u.Game.Value.Name)
                .GroupBy(n => n)
                .OrderByDescending(g => g.Count());

            var gameState = new List<object>();

            foreach (var game in gameNames) 
            {
                gameState.Add(new {
                    name = game.Key,
                    count = game.Count()
                });
            }

            var postContent = new Dictionary<string, string>();
            postContent.Add("gamestate", JsonConvert.SerializeObject(gameState));

            using (var client = new HttpClient()) 
            {
                client.DefaultRequestHeaders.Add("user-key", _options["gameStateEmissions:emissionEndpointUserKey"]);
                var response = await client.PostAsync(_options["gameStateEmissions:emissionEndpoint"], new FormUrlEncodedContent(postContent));
                var responseContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine(responseContent);
            }
        }
    }
}