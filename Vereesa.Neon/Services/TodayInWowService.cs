using Discord;
using Vereesa.Neon.Integrations.Interfaces;
using Discord.Interactions;
using Vereesa.Core;

namespace Vereesa.Neon.Services
{
    public class TodayInWoWService : IBotService
    {
        private IWowheadClient _wowhead;

        public TodayInWoWService(IWowheadClient wowhead)
        {
            _wowhead = wowhead;
        }

        [SlashCommand("elemental-storm", "Show current elemental storm")]
        public async Task Test(IDiscordInteraction interaction)
        {
            var currentElementalStorm = await _wowhead.GetCurrentElementalStorm();

            if (currentElementalStorm == null)
            {
                await interaction.RespondAsync("No elemental storms found.");
                return;
            }

            var embed = new EmbedBuilder()
                .WithTitle("Current Elemental Storm")
                .AddField("Type", currentElementalStorm.Type, true)
                .AddField("Zone", currentElementalStorm.Zone, true)
                .AddField("Status", currentElementalStorm.Status, true)
                .WithFooter($"Ending at {currentElementalStorm.EndingAt:HH:mm} (UTC)");

            await interaction.RespondAsync(embed: embed.Build());
        }
    }
}
