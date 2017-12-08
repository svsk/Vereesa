using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;

namespace Vereesa.Core.Services
{
    public class StartupService
    {
        private readonly DiscordSocketClient _discord;
        private readonly IConfigurationRoot _config;

        public StartupService(DiscordSocketClient discord, IConfigurationRoot config)
        {
            _config = config;
            _discord = discord;
        }

        public async Task StartAsync()
        {
            string discordToken = _config["tokens:discord"];

            if (string.IsNullOrWhiteSpace(discordToken))
                throw new Exception("Please enter your bot's token into the `_configuration.json` file found in the applications root directory.");

            await _discord.LoginAsync(TokenType.Bot, discordToken);
            await _discord.StartAsync();
        }
    }
}