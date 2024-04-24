using System.ComponentModel;
using Discord;
using Discord.Interactions;
using Vereesa.Core;
using Vereesa.Neon.Exceptions;
using Vereesa.Neon.Services;

namespace Vereesa.Neon.Modules;

public class NeonCoinModule : IBotModule
{
    private readonly NeonCoinService _neonCoinService;

    public NeonCoinModule(NeonCoinService neonCoinService) => _neonCoinService = neonCoinService;

    [SlashCommand("check-coin", "Check your NeonCoin balance!")]
    public async Task CheckBalanceAsync(IDiscordInteraction interaction)
    {
        var author = interaction.User;
        var currentHoldings = _neonCoinService.GetUserCoinHoldings(author.Id);

        var response = $"💰 You have {currentHoldings.CoinCount} Neon€oins, {author.Mention}!";
        response += $"\n💸 That's €{currentHoldings.EuroWorth} in real money!";

        await interaction.RespondAsync(response);
    }

    [SlashCommand("mint-coin", "Generates a NeonCoin for you!")]
    public async Task MintCoin(IDiscordInteraction interaction)
    {
        var author = interaction.User;

        try
        {
            _neonCoinService.GenerateCoin(author.Id);
            var currentHoldings = _neonCoinService.GetUserCoinHoldings(author.Id);

            var response =
                $"🪙 Here's a freshly minted Neon€oin for you, {author.Mention}! You now have {currentHoldings.CoinCount} N€.";
            response += $"\n💰 That's €{currentHoldings.EuroWorth} in real money!";

            await interaction.RespondAsync(response);
        }
        catch (OperationThrottledException ex)
        {
            await interaction.RespondAsync($"🚫 I can only mint 1 coin every {ex.MinWaitTime.TotalMinutes} minutes.");
        }
    }

    [SlashCommand("tip-coin", "Tip a NeonCoin to another user!")]
    public async Task TipCoinAsync(
        IDiscordInteraction interaction,
        [Description("The user to tip the coin to.")] IUser user
    )
    {
        var author = interaction.User;

        try
        {
            _neonCoinService.TipCoin(author.Id, user.Id);
            var currentHoldings = _neonCoinService.GetUserCoinHoldings(author.Id);

            var response =
                $"🪙 You tipped a Neon€oin to {user.Mention}! You now have {currentHoldings.CoinCount} N€ left.";

            await interaction.RespondAsync(response);
        }
        catch (InsufficientFundsException)
        {
            await interaction.RespondAsync("🚫 You don't have enough Neon€oins to tip!");
        }
    }
}
