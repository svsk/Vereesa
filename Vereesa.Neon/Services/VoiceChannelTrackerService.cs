using Discord;
using Microsoft.Extensions.Logging;
using Vereesa.Neon.Configuration;
using Vereesa.Neon.Helpers;
using Vereesa.Core.Infrastructure;
using Timer = System.Timers.Timer;
using Vereesa.Core;

namespace Vereesa.Neon.Services
{
    public class VoiceChannelTrackerService : IBotService
    {
        private readonly IMessagingClient _messaging;
        private readonly ILogger<VoiceChannelTrackerService> _logger;
        private readonly ulong _announcementChannelId;
        private readonly Timer _clearOldMessagesInterval;

        public VoiceChannelTrackerService(
            IMessagingClient messaging,
            VoiceChannelTrackerSettings settings,
            ILogger<VoiceChannelTrackerService> logger
        )
        {
            _messaging = messaging;
            _logger = logger;

            _announcementChannelId = settings.AnnouncementChannelId;
            _clearOldMessagesInterval = TimerHelpers.SetTimeout(
                async () =>
                {
                    await DeleteOldMessages();
                },
                120000,
                true
            );
        }

        [OnReady]
        public async Task InitializeVoiceChannelTracker()
        {
            await DeleteOldMessages();
        }

        [OnVoiceStateChange]
        public async Task HandleVoiceStateChanged(
            IUser user,
            IVoiceState beforeChangeState,
            IVoiceState afterChangeState
        )
        {
            var beforeChannel = beforeChangeState.VoiceChannel;
            var afterChannel = afterChangeState.VoiceChannel;

            var action = string.Empty;
            if (beforeChannel != null && afterChannel == null)
            {
                action = $"left `{beforeChannel.Name}`";
            }

            if (beforeChannel != null && afterChannel != null)
            {
                action = $"switched from `{beforeChannel.Name}` to `{afterChannel.Name}`";
            }

            if (beforeChannel == null && afterChannel != null)
            {
                action = $"joined `{afterChannel.Name}`";
            }

            if (beforeChannel?.Id == afterChannel?.Id)
                return;

            await SendTTSMessage($"{user.Username} {action}.");
        }

        private IMessageChannel GetChannel() => _messaging.GetChannelById(_announcementChannelId) as IMessageChannel;

        private async Task SendTTSMessage(string message)
        {
            var channel = GetChannel();
            if (channel == null)
                return;

            await channel.SendMessageAsync(message, isTTS: true);
        }

        private async Task DeleteOldMessages()
        {
            var channel = GetChannel();
            if (channel == null)
                return;

            try
            {
                var messages = await channel.GetMessagesAsync(1000).Flatten().ToListAsync();
                foreach (var message in messages)
                {
                    if (message.Timestamp < DateTimeOffset.UtcNow.AddHours(-3))
                    {
                        await message.DeleteAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, ex);
            }
        }
    }
}
