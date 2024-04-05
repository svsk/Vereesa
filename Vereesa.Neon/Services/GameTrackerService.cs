using System.Net;
using System.Net.Http.Headers;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Vereesa.Neon.Configuration;
using Vereesa.Core.Infrastructure;
using Vereesa.Neon.Data.Interfaces;
using Vereesa.Neon.Data.Models.GameTracking;
using Vereesa.Core;

namespace Vereesa.Neon.Services
{
    public class GameTrackerService : IBotModule
    {
        private IMessagingClient _messaging;
        private IRepository<GameTrackMember> _trackingRepo;
        private GameStateEmissionSettings _options;
        private ILogger<GameTrackerService> _logger;

        /// This service is fully async
        public GameTrackerService(
            IMessagingClient messaging,
            IRepository<GameTrackMember> trackingRepo,
            GameStateEmissionSettings emissionSettings,
            ILogger<GameTrackerService> logger
        )
        {
            _messaging = messaging;
            _trackingRepo = trackingRepo;
            _options = emissionSettings;
            _logger = logger;
        }

        [OnReady]
        public async Task OnReady()
        {
            var guilds = _messaging.GetServers();
            foreach (var guild in guilds)
            {
                await EmitGameState(guild);
            }
        }

        [OnMemberUpdated]
        [AsyncHandler]
        public async Task OnMemberUpdatedAsync(SocketGuildUser userBeforeChange, SocketGuildUser userAfterChange)
        {
            var gameChangeHappened = false;
            var beforeGame = userBeforeChange.Activities.FirstOrDefault();
            var afterGame = userAfterChange.Activities.FirstOrDefault();

            try
            {
                if (beforeGame != null && (afterGame == null || afterGame.Name != beforeGame.Name))
                {
                    gameChangeHappened = true;
                    _logger.LogInformation($"{userBeforeChange.Username} stopped playing {beforeGame.Name}.");
                    await UpdateUserGameState(userBeforeChange, "stopped");
                }

                if (afterGame != null && (beforeGame == null || beforeGame.Name != afterGame.Name))
                {
                    gameChangeHappened = true;
                    _logger.LogInformation($"{userBeforeChange.Username} started playing {afterGame.Name}.");
                    await UpdateUserGameState(userAfterChange, "started");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to update player game history.", ex);
            }

            if (gameChangeHappened)
            {
                _logger.LogInformation("Emitting game state.");
                await EmitGameState(userAfterChange.Guild);
            }
        }

        private async Task UpdateUserGameState(SocketGuildUser user, string eventType)
        {
            var userHistory = await GetGameTrackMemberAsync(user);
            var gameName = user.Activities.FirstOrDefault()?.Name;

            var gameHistory = userHistory.GetGameHistory(gameName);
            gameHistory.Add(GameTrackEntry.CreateInstance(eventType));

            await _trackingRepo.AddOrEditAsync(userHistory);
            await _trackingRepo.SaveAsync();
        }

        private async Task<GameTrackMember> GetGameTrackMemberAsync(SocketGuildUser user)
        {
            var member = await _trackingRepo.FindByIdAsync(user.Id.ToString());
            if (member == null)
            {
                member = new GameTrackMember();
                member.Id = user.Id.ToString();
                member.Username = user.Username;
                member.GameHistory = new Dictionary<string, ICollection<GameTrackEntry>>();
                await _trackingRepo.AddAsync(member);
            }

            return member;
        }

        private async Task EmitGameState(IGuild guild)
        {
            var gameNames = _messaging
                .GetServerUsersById(guild.Id)
                .Where(u => u.Activities.Any() && u.IsBot == false)
                .SelectMany(u => u.Activities.Select(a => a.Name))
                .GroupBy(n => n)
                .OrderByDescending(g => g.Count());

            var gameState = new List<object>();

            foreach (var game in gameNames)
            {
                gameState.Add(new { name = game.Key, count = game.Count() });
            }

            var serializedGameState = JsonConvert.SerializeObject(gameState, Formatting.Indented);

            _logger.LogInformation("Emitting the following game state");
            _logger.LogInformation(serializedGameState);

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
                    _logger.LogWarning("Failed to emit game state.", responseContent);
                }
            }
        }
    }
}
