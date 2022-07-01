using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Vereesa.Core.Infrastructure;

namespace Vereesa.Core.Services
{
	public class NeonCoinService : BotServiceBase
	{
		private static Dictionary<ulong, int> _coinStandings = new();
		private readonly Random _rng;

		public NeonCoinService(DiscordSocketClient discord, Random rng)
			: base(discord)
		{
			_rng = rng;
		}

		[OnCommand("!givecoin")]
		public async Task HandleMessageAsync(IMessage message)
		{
			if (!_coinStandings.ContainsKey(message.Author.Id))
			{
				_coinStandings.TryAdd(message.Author.Id, 1);
			}
			else
			{
				_coinStandings[message.Author.Id] = _coinStandings[message.Author.Id] + 1;
			}

			var currentStanding = (decimal)_coinStandings[message.Author.Id];

			var response = $"🪙 Here's a Neon€oin for you, {message.Author.Username}! You now have N€{currentStanding}.";
			response += $"\n💰 That's €{Math.Ceiling((_rng.Next(2, 3000) / 100) * currentStanding)} in real money!";

			await message.Channel.SendMessageAsync(response);
		}
	}
}
