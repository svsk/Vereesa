using System;
using System.Threading.Tasks;
using System.Timers;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Vereesa.Core.Configuration;

namespace Vereesa.Core.Services
{
    public class StartupService
    {
        private readonly DiscordSocketClient _discord;
        private readonly DiscordSettings _settings;

        public StartupService(DiscordSocketClient discord, DiscordSettings settings)
        {
            _settings = settings;
            _discord = discord;

            _discord.Disconnected -= HandleDisconnected;
            _discord.Disconnected += HandleDisconnected;
            _discord.Connected -= HandleConnected;
            _discord.Connected += HandleConnected;
        }

        private Timer _reconnectAttemptTimer = null;

        public async Task StartAsync()
        {
            var discordToken = _settings.Token;

            if (string.IsNullOrWhiteSpace(discordToken))
                throw new Exception("Please enter your bot's token into the `config.json` file found in the applications root directory.");

            await _discord.LoginAsync(TokenType.Bot, discordToken);
            await _discord.StartAsync();
        }

        private async Task HandleDisconnected(Exception arg)
        {
            Console.WriteLine("Disconnected!");

            if (_reconnectAttemptTimer == null)
            {
                _reconnectAttemptTimer = new Timer(5000);
                _reconnectAttemptTimer.Elapsed += AttemptReconnect;
                _reconnectAttemptTimer.AutoReset = true;
                _reconnectAttemptTimer.Start();
            }
        }

        private async void AttemptReconnect(object sender, ElapsedEventArgs e)
        {
            Console.WriteLine("Attempting reconnect...");
            await this.StartAsync();
        }

        private async Task HandleConnected() 
        {
            Console.WriteLine("Connected!");

            if (_reconnectAttemptTimer != null) 
            {
                _reconnectAttemptTimer.Stop();
            }
        }
    }
}