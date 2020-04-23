using System;
using System.Threading.Tasks;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;

namespace Vereesa.Core.Infrastructure
{
    public class DiscordChannelLogger : ILogger
    {
        private string _category;
        private DiscordSocketClient _discord;
        private ISocketMessageChannel _channel;
        private LogLevel _logLevel;
        private ulong _channelId;

        public DiscordChannelLogger(string categoryName, DiscordSocketClient discord, ulong channelId, LogLevel logLevel) 
        {
            _category = categoryName;
            _discord = discord;
            _discord.Ready -= EnableLogger;
            _discord.Ready += EnableLogger;
            _channelId = _channelId;
            _logLevel = logLevel;
        }

        private Task EnableLogger()
        {
            _channel = (ISocketMessageChannel)_discord?.GetChannel(_channelId);
            return Task.CompletedTask;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return _channel != null;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (logLevel >= _logLevel)
                _channel?.SendMessageAsync($"`{_category}`: {state.ToString()} {exception}");
        }
    }
}