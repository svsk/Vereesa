using NodaTime;
using Vereesa.Neon.Extensions;
using Vereesa.Core.Infrastructure;
using Vereesa.Core;

namespace Vereesa.Neon.Services;

public class CountdownService : IBotModule
{
    private static bool _started = false;
    private readonly IEmojiClient _emoji;
    private readonly IMessagingClient _messaging;
    private readonly IJobScheduler _scheduler;

    private ulong _channelId = 326097353633300480;

    private Instant NeonCon2022 =>
        Instant.FromDateTimeOffset(new DateTimeOffset(2022, 6, 8, 7, 0, 0, new TimeSpan(2, 0, 0)));

    public CountdownService(IMessagingClient messaging, IEmojiClient emoji, IJobScheduler scheduler)
    {
        _emoji = emoji;
        _messaging = messaging;
        _scheduler = scheduler;
    }

    [OnReady]
    public Task ScheduleAnnouncements()
    {
        if (_started)
        {
            return Task.CompletedTask;
        }

        ScheduleNeonConCountdown();
        _started = true;

        return Task.CompletedTask;
    }

    private void ScheduleNeonConCountdown()
    {
        var now = SystemClock.Instance.GetCurrentInstant();
        if (NeonCon2022 > now)
        {
            var sixOClockToday = now.AsServerTime().Date.AtStartOfDayInZone(TzHelper.ServerTz).PlusHours(7).ToInstant();

            var sixOClockTomorrow = now.AsServerTime()
                .PlusHours(24)
                .Date.AtStartOfDayInZone(TzHelper.ServerTz)
                .PlusHours(7)
                .ToInstant();

            var nextSixOClock = now < sixOClockToday ? sixOClockToday : sixOClockTomorrow;

            _scheduler.Schedule(nextSixOClock, async () => await AnnounceTimeUntilNeonCon());
        }
    }

    private async Task AnnounceTimeUntilNeonCon()
    {
        var now = SystemClock.Instance.GetCurrentInstant();
        var timeUntil = NeonCon2022 - now;

        if (now >= NeonCon2022)
        {
            await _messaging.SendMessageToChannelByIdAsync(
                _channelId,
                $"💥{_emoji.GetCustomEmoji("Poggers")}{_emoji.GetCustomEmoji("Neon")}🎉 "
                    + $"NeonCon 2022 is today my dudes!"
                    + $" 💥{_emoji.GetCustomEmoji("Poggers")}{_emoji.GetCustomEmoji("Neon")}🎉"
            );
        }
        else
        {
            await _messaging.SendMessageToChannelByIdAsync(
                _channelId,
                $"⏱️{_emoji.GetCustomEmoji("PauseChamp")}{_emoji.GetCustomEmoji("Neon")}🎉 "
                    + $"Only {timeUntil.Days} days, {timeUntil.Hours} hours, and {timeUntil.Minutes} minutes until NeonCon 2022!"
            );

            ScheduleNeonConCountdown();
        }
    }
}
