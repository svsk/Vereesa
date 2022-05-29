using System;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using NodaTime;
using Vereesa.Core.Extensions;
using Vereesa.Core.Infrastructure;

namespace Vereesa.Core.Services;

public class CountdownService : BotServiceBase
{
	private static bool _started = false;
	private readonly IJobScheduler _scheduler;

	private IMessageChannel Channel => ((IMessageChannel)this.Discord.GetChannel(326097353633300480));
	private Instant NeonCon2022 => Instant.FromDateTimeOffset(new DateTimeOffset(2022, 6, 8, 7, 0, 0, new TimeSpan(2, 0, 0)));

	public CountdownService(DiscordSocketClient discord, IJobScheduler scheduler)
		: base(discord)
	{
		_scheduler = scheduler;
	}

	[OnReady]
	public async Task ScheduleAnnouncements()
	{
		if (_started) return;
		ScheduleNeonConCountdown();
		_started = true;
	}

	private void ScheduleNeonConCountdown()
	{
		var now = SystemClock.Instance.GetCurrentInstant();
		if (NeonCon2022 > now)
		{
			var sixOClockToday = now.AsServerTime().Date
				.AtStartOfDayInZone(TzHelper.ServerTz)
				.PlusHours(7)
				.ToInstant();

			var sixOClockTomorrow = now.AsServerTime().PlusHours(24).Date
				.AtStartOfDayInZone(TzHelper.ServerTz)
				.PlusHours(7)
				.ToInstant();

			var nextSixOClock = now < sixOClockToday
				? sixOClockToday
				: sixOClockTomorrow;

			_scheduler.Schedule(nextSixOClock, async () => await AnnounceTimeUntilNeonCon());
		}
	}

	private async Task AnnounceTimeUntilNeonCon()
	{
		var now = SystemClock.Instance.GetCurrentInstant();
		var timeUntil = NeonCon2022 - now;

		if (now >= NeonCon2022)
		{
			await Channel.SendMessageAsync(
				$"💥{Discord.GetNeonEmoji("Poggers")}{Discord.GetNeonEmoji("Neon")}🎉 " +
				$"NeonCon 2022 is today my dudes!" +
				$" 💥{Discord.GetNeonEmoji("Poggers")}{Discord.GetNeonEmoji("Neon")}🎉"
			);
		}
		else
		{
			await Channel.SendMessageAsync(
				$"⏱️{Discord.GetNeonEmoji("PauseChamp")}{Discord.GetNeonEmoji("Neon")}🎉 " +
				$"Only {timeUntil.Days} days, {timeUntil.Hours} hours, and {timeUntil.Minutes} minutes until NeonCon 2022!"
			);

			ScheduleNeonConCountdown();
		}
	}
}
