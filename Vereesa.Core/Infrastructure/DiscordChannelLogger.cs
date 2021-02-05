using System;
using System.Collections.Generic;
using System.Linq;
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
		private List<string> _preloadMessages = new List<string>();

		public DiscordChannelLogger(string categoryName, DiscordSocketClient discord, ulong channelId, LogLevel logLevel)
		{
			_category = categoryName;
			_discord = discord;
			_discord.Ready -= EnableLogger;
			_discord.Ready += EnableLogger;
			_channelId = channelId;
			_logLevel = logLevel;
		}

		private async Task EnableLogger()
		{
			_channel = (ISocketMessageChannel)_discord?.GetChannel(_channelId);

			foreach (var message in _preloadMessages)
			{
				await _channel?.SendMessageAsync(message);
			}
		}

		public IDisposable BeginScope<TState>(TState state)
		{
			return null;
		}

		public bool IsEnabled(LogLevel logLevel) => true;


		public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
		{
			if (logLevel < _logLevel)
			{
				return;
			}

			var message = $"`{_category} ({logLevel})`: {state.ToString()} {exception}";
			message = message.Length > 2000 ? message.Substring(0, 1999) + "…" : message;

			if (_channel == null)
			{
				_preloadMessages.Add(message);
			}
			else
			{
				_channel?.SendMessageAsync(message);
			}
		}
	}
}