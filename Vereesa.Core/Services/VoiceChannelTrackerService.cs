using System;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using Vereesa.Core.Configuration;
using Vereesa.Core.Extensions;
using Vereesa.Core.Helpers;

namespace Vereesa.Core.Services
{
    public class VoiceChannelTrackerService
    {
        private ILogger<VoiceChannelTrackerService> _logger;
        private DiscordSocketClient _discord;
        private VoiceChannelTrackerSettings _settings;
        private ISocketMessageChannel _announcementChannel;
        private Timer _clearOldMessagesInterval;

        public VoiceChannelTrackerService(DiscordSocketClient discord, VoiceChannelTrackerSettings settings, ILogger<VoiceChannelTrackerService> logger)
        {
            _logger = logger;
            _discord = discord;
            _discord.GuildAvailable += InitailizeVoiceChannelTracker;
            _settings = settings;
            _clearOldMessagesInterval = TimerHelpers.SetTimeout(async () => { await DeleteOldMessages(); }, 120000, true);
        }

        private async Task DeleteOldMessages()
        {
            if (_announcementChannel == null)
                return;

            try 
            {
                var messages = _announcementChannel.GetMessagesAsync(1000).Flatten();
                messages.ForEach(msg => {
                    if (msg.Timestamp < DateTimeOffset.UtcNow.AddHours(-3)) {
                        msg.DeleteAsync().GetAwaiter().GetResult();
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, ex);
            }
            
        }

        private async Task InitailizeVoiceChannelTracker(SocketGuild guild)
        {
            _discord.UserVoiceStateUpdated -= HandleVoiceStateChanged;
            _discord.UserVoiceStateUpdated += HandleVoiceStateChanged;
            _announcementChannel = guild.GetChannelByName(_settings.AnnouncementChannelName);
            await DeleteOldMessages();
        }

        private async Task HandleVoiceStateChanged(SocketUser user, SocketVoiceState beforeChangeState, SocketVoiceState afterChangeState)
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

            await _announcementChannel.SendMessageAsync($"{user.Username} {action}.", isTTS: true);
        }
    }
}