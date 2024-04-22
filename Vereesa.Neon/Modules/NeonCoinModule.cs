using Discord;
using Discord.Interactions;
using Vereesa.Core;
using Vereesa.Neon.Services;

namespace Vereesa.Neon.Modules;

public class NeonCoinModule : IBotModule
{
    private readonly NeonCoinService _neonCoinService;

    public NeonCoinModule(NeonCoinService neonCoinService) => _neonCoinService = neonCoinService;

    [SlashCommand("give-coin", "Generates a NeonCoin for you!")]
    public async Task HandleMessageAsync(IDiscordInteraction interaction)
    {
        var author = interaction.User;

        _neonCoinService.GenerateCoin(author.Id);
        var currentHoldings = _neonCoinService.GetUserCoinHoldings(author.Id);

        var response = $"🪙 Here's a Neon€oin for you, {author.Username}! You now have {currentHoldings.CoinCount} N€.";
        response += $"\n💰 That's €{currentHoldings.EuroWorth} in real money!";

        await interaction.RespondAsync(response);
    }
}
