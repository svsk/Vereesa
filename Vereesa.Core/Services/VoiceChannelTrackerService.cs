using System;
using System.Threading.Tasks;
using Discord.WebSocket;
using Vereesa.Core.Configuration;
using Vereesa.Core.Extensions;

namespace Vereesa.Core.Services
{
    public class VoiceChannelTrackerService
    {
        private DiscordSocketClient _discord;
        private VoiceChannelTrackerSettings _settings;
        private ISocketMessageChannel _announcementChannel;

        public VoiceChannelTrackerService(DiscordSocketClient discord, VoiceChannelTrackerSettings settings) 
        {
            _discord = discord;
            _discord.GuildAvailable += InitailizeVoiceChannelTracker;
            _settings = settings;
        }

        private async Task InitailizeVoiceChannelTracker(SocketGuild guild)
        {
            _discord.UserVoiceStateUpdated -= HandleVoiceStateChanged;
            _discord.UserVoiceStateUpdated += HandleVoiceStateChanged;
            _announcementChannel = guild.GetChannelByName(_settings.AnnouncementChannelName);
        }

        private async Task HandleVoiceStateChanged(SocketUser user, SocketVoiceState beforeChangeState, SocketVoiceState afterChangeState)
        {
            var beforeChannel = beforeChangeState.VoiceChannel;
            var afterChannel = afterChangeState.VoiceChannel;

            var action = string.Empty;
            if (beforeChannel != null && afterChannel == null) {
                action = $"left `{beforeChannel.Name}`";
            }
                
            if (beforeChannel != null && afterChannel != null) {
                action = $"switched from `{beforeChannel.Name}` to `{afterChannel.Name}`";
            }
                
            if (beforeChannel == null && afterChannel != null) {
                action = $"joined `{afterChannel.Name}`";
            }

            await _announcementChannel.SendMessageAsync($"{user.Username} {action}.", isTTS: true);
        }
    }
}