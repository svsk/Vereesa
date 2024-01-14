using Discord;
using Discord.Interactions;
using Microsoft.Extensions.Logging;
using Vereesa.Core;
using Vereesa.Core.Infrastructure;

namespace Vereesa.TestBot;

public class TestService : IBotService
{
    public TestService(ILogger<TestService> logger)
    {
        logger.LogInformation("Started TestService!");
    }

    [OnCommand("!ping")]
    public async Task LegacyPing(IMessage message)
    {
        await message.Channel.SendMessageAsync("Pong!");
    }

    [SlashCommand("ping", "pong")]
    public async Task Ping(IDiscordInteraction interaction)
    {
        await interaction.RespondAsync("Plong!");
    }

    [SlashCommand("greet", "Greet someone!")]
    public async Task Greet(IDiscordInteraction interaction, string name)
    {
        if (name == null)
        {
            await interaction.RespondAsync($"Hello!");
        }
        else
        {
            await interaction.RespondAsync($"Hello, {name}!");
        }
    }

    [SlashCommand("yeet", "Yeet someone!")]
    public async Task Yeet(IDiscordInteraction interaction, string name, string from, bool overhead)
    {
        var method = overhead ? "overhead" : "underhand";
        await interaction.RespondAsync($"Yeeted {name} from {from} with an {method} throw!");
    }
}
