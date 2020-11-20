using System;
using System.Threading.Tasks;
using System.Timers;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using Vereesa.Core.Configuration;
using Vereesa.Core.Infrastructure;

namespace Vereesa.Core.Services
{
	public class StartupService : BotServiceBase
	{
		private readonly ILogger<StartupService> _logger;
		private readonly DiscordSettings _settings;

		public StartupService(DiscordSocketClient discord, DiscordSettings settings, ILogger<StartupService> logger)
			: base(discord)
		{
			_settings = settings;
			_logger = logger;

			// _discord.Disconnected -= HandleDisconnected;
			// _discord.Disconnected += HandleDisconnected;
			// _discord.Connected -= HandleConnected;
			// _discord.Connected += HandleConnected;
			discord.Log += HandleLogMessage;
			discord.Ready += () =>
			{
				_logger.LogWarning("Ready was invoked.");
				return Task.CompletedTask;
			};
		}

		private Timer _reconnectAttemptTimer = null;

		public async Task StartAsync()
		{
			var discordToken = _settings.Token;

			if (string.IsNullOrWhiteSpace(discordToken))
				throw new Exception("Please enter your bot's token into the `config.json` file found in the applications root directory.");

			await Discord.LoginAsync(TokenType.Bot, discordToken);
			await Discord.StartAsync();
		}

		private Task HandleDisconnected(Exception arg)
		{
			_logger.LogInformation("Disconnected!");

			if (_reconnectAttemptTimer == null)
			{
				_logger.LogInformation("Attempting to reconnect in 5 seconds.");
				_reconnectAttemptTimer = new Timer(5000);
				_reconnectAttemptTimer.Elapsed += AttemptReconnect;
				_reconnectAttemptTimer.AutoReset = true;
				_reconnectAttemptTimer.Start();
			}

			return Task.CompletedTask;
		}

		private async void AttemptReconnect(object sender, ElapsedEventArgs e)
		{
			_logger.LogInformation("Attempting reconnect...");
			await this.StartAsync();
		}

		private Task HandleConnected()
		{
			_logger.LogInformation("Connected!");

			if (_reconnectAttemptTimer != null)
			{
				_reconnectAttemptTimer.Stop();
			}

			return Task.CompletedTask;
		}

		private Task HandleLogMessage(LogMessage logMessage)
		{
			switch (logMessage.Severity)
			{
				case LogSeverity.Verbose:
					_logger.Log(LogLevel.Debug, logMessage.Message, logMessage.Exception);
					break;
				case LogSeverity.Debug:
					_logger.Log(LogLevel.Debug, logMessage.Message, logMessage.Exception);
					break;
				case LogSeverity.Info:
					_logger.Log(LogLevel.Information, logMessage.Message, logMessage.Exception);
					break;
				case LogSeverity.Warning:
					_logger.Log(LogLevel.Warning, logMessage.Message, logMessage.Exception);
					break;
				case LogSeverity.Error:
					_logger.Log(LogLevel.Error, logMessage.Message, logMessage.Exception);
					break;
				case LogSeverity.Critical:
					_logger.Log(LogLevel.Critical, logMessage.Message, logMessage.Exception);
					break;
				default:
					_logger.LogWarning($"Unknown severity level detected ({logMessage.Severity}).");
					_logger.Log(LogLevel.Information, logMessage.Message, logMessage.Exception);
					break;
			}

			return Task.CompletedTask;
		}
	}
}