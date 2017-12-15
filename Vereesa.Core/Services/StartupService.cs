using System;
using System.Threading.Tasks;
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
        }

        public async Task StartAsync()
        {
            var discordToken = _settings.Token;

            if (string.IsNullOrWhiteSpace(discordToken))
                throw new Exception("Please enter your bot's token into the `config.json` file found in the applications root directory.");

            await _discord.LoginAsync(TokenType.Bot, discordToken);
            await _discord.StartAsync();
        }
    }
}