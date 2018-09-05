using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Vereesa.Core.Configuration;
using Vereesa.Data.Models.GameTracking;
using Vereesa.Data.Repositories;

namespace Vereesa.Core.Services
{
    public class GameTrackerService
    {
        private DiscordSocketClient _discord;
        private JsonRepository<GameTrackMember> _trackingRepo;
        private GameStateEmissionSettings _options;

        public GameTrackerService(DiscordSocketClient discord, JsonRepository<GameTrackMember> trackingRepo, GameStateEmissionSettings emissionSettings)
        {
            _discord = discord;
            _trackingRepo = trackingRepo;
            _discord.GuildAvailable += OnGuildAvailable;
            _discord.GuildMemberUpdated += OnMemberUpdated;
            _options = emissionSettings;
        }

        private async Task OnGuildAvailable(SocketGuild guild)
        {
            await EmitGameState(guild);
        }

        private async Task OnMemberUpdated(SocketGuildUser userBeforeChange, SocketGuildUser userAfterChange)
        {
            var gameChangeHappened = false;
            var beforeGame = userBeforeChange.Activity;
            var afterGame = userAfterChange.Activity;

            if (beforeGame != null && (afterGame == null || afterGame.Name != beforeGame.Name))
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
            var gameName = user.Activity.Name;
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

        private async Task EmitGameState(SocketGuild guild)
        {
            var gameNames = guild.Users.Where(u => u.Activity != null && u.IsBot == false)
                .Select(u => u.Activity.Name)
                .GroupBy(n => n)
                .OrderByDescending(g => g.Count());

            var gameState = new List<object>();

            foreach (var game in gameNames)
            {
                gameState.Add(new
                {
                    name = game.Key,
                    count = game.Count()
                });
            }

            var serializedGameState = JsonConvert.SerializeObject(gameState);
            var buffer = System.Text.Encoding.UTF8.GetBytes(serializedGameState);
            var byteGamestate = new ByteArrayContent(buffer);
            byteGamestate.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Authorization", _options.EmissionEndpointUserKey);
                var response = await client.PostAsync(_options.EmissionEndpoint, byteGamestate);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.StatusCode != HttpStatusCode.OK)
                {
                    Console.WriteLine(responseContent);
                }
            }
        }
    }
}