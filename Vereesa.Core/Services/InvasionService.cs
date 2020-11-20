using System;
using System.Globalization;
using System.Threading.Tasks;
using Discord.WebSocket;
using NodaTime;
using Vereesa.Core.Extensions;
using Vereesa.Core.Infrastructure;

namespace Vereesa.Core.Services
{
	public class InvasionService : BotServiceBase
	{
		private DiscordSocketClient _discord;

		public InvasionService(DiscordSocketClient discord)
			: base(discord)
		{
			_discord = discord;

			_discord.MessageReceived -= HandleMessageReceivedAsync;
			_discord.MessageReceived += HandleMessageReceivedAsync;
		}

		private string FormatDuration(Duration duration) => duration.ToString("H' hours and 'm' minutes'", CultureInfo.InvariantCulture);

		private async Task HandleMessageReceivedAsync(SocketMessage message)
		{
			if (message?.Content == "!invasion")
			{
				await message.Channel.SendMessageAsync(GetInvasionStatus(SystemClock.Instance.GetCurrentInstant()));
			}
		}

		private string GetInvasionStatus(Instant currentInstant)
		{
			var cycleDuration = Duration.FromHours(18.5); // Total cycle duration (Invasion duration + downtime)
			var invasionDuration = Duration.FromHours(6);
			var currentPointInCycle = Duration.FromSeconds((currentInstant.ToUnixTimeSeconds() - 1800) % cycleDuration.TotalSeconds);

			if (currentPointInCycle < invasionDuration)
			{
				var untilInvasionEnd = invasionDuration.Minus(currentPointInCycle);
				return $":smiling_imp: There's a Legion invasion going on right now! It will end in {FormatDuration(untilInvasionEnd)}.";
			}
			else
			{
				var untilInvasionBegin = cycleDuration.Minus(currentPointInCycle);
				var invasionEnd = currentInstant.Plus(untilInvasionBegin).Plus(invasionDuration);

				return $":hourglass_flowing_sand: The next Legion invasion is in {FormatDuration(untilInvasionBegin)}. It will end at {invasionEnd.ToDateTimeOffset().ToCentralEuropeanTime().ToString("HH:mm")} server time.";
			}
		}
	}
}