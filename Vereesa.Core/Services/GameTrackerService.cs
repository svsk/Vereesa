using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Vereesa.Core.Configuration;
using Vereesa.Core.Infrastructure;
using Vereesa.Data.Interfaces;
using Vereesa.Data.Models.GameTracking;

namespace Vereesa.Core.Services
{
	public class GameTrackerService : BotServiceBase
	{
		private DiscordSocketClient _discord;
		private IRepository<GameTrackMember> _trackingRepo;
		private GameStateEmissionSettings _options;
		private ILogger<GameTrackerService> _logger;

		/// This service is fully async
		public GameTrackerService(DiscordSocketClient discord, IRepository<GameTrackMember> trackingRepo, GameStateEmissionSettings emissionSettings, ILogger<GameTrackerService> logger)
			: base(discord)
		{
			_discord = discord;
			_trackingRepo = trackingRepo;
			_discord.GuildAvailable += OnGuildAvailableAsync;
			_options = emissionSettings;
			_logger = logger;
		}

		private async Task OnGuildAvailableAsync(SocketGuild guild)
		{
			await EmitGameState(guild);
		}

		[OnMemberUpdated]
		[AsyncHandler]
		public async Task OnMemberUpdatedAsync(SocketGuildUser userBeforeChange, SocketGuildUser userAfterChange)
		{
			var gameChangeHappened = false;
			var beforeGame = userBeforeChange.Activity;
			var afterGame = userAfterChange.Activity;

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
			var gameName = user.Activity.Name;

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