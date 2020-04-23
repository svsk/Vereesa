using Discord.WebSocket;
using Microsoft.Extensions.Logging;

namespace Vereesa.Core.Infrastructure
{
    public class DiscordChannelLoggerProvider : ILoggerProvider
    {
        private DiscordSocketClient _discord;
        private ulong _channelId;
        private LogLevel _logLevel;

        public DiscordChannelLoggerProvider(DiscordSocketClient discord, ulong channelId, LogLevel logLevel)
        {
            _discord = discord;
            _channelId = channelId;
            _logLevel = logLevel;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new DiscordChannelLogger(categoryName, _discord, _channelId, _logLevel);
        }

        public void Dispose()
        {
            // Not sure if we should dispose the _discord client... Probably not...?
        }
    }
}