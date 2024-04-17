using System.ComponentModel;
using Discord;
using Discord.Interactions;
using Microsoft.Extensions.Logging;
using Vereesa.Core;
using Vereesa.Neon.Data;
using Vereesa.Neon.Services;

namespace Vereesa.Neon.Modules;

public class TimeZoneModule : IBotModule
{
    private readonly TimeZoneService _service;
    private readonly ILogger<TimeZoneModule> _logger;

    public TimeZoneModule(TimeZoneService service, ILogger<TimeZoneModule> logger)
    {
        _service = service;
        _logger = logger;
    }

    [SlashCommand("set-timezone", "Set your preferred time zone.")]
    public async Task SetUserTimeZone(
        IDiscordInteraction interaction,
        [Description("Your preferred time zone")]
        [Choice("Europe/Oslo", "Europe/Oslo"), Choice("Europe/London", "Europe/London")]
            string timeZone
    )
    {
        await interaction.DeferAsync();

        try
        {
            await _service.SetUserTimezoneSettingsAsync(interaction.User.Id, timeZone);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set user timezone");
            await interaction.FollowupAsync("Failed to set your timezone.");
            return;
        }

        await interaction.FollowupAsync("Your timezone is now " + timeZone);
    }

    [SlashCommand("now", "Get the current time in your preferred timezone")]
    public async Task GetTimeForUser(IDiscordInteraction interaction)
    {
        await interaction.DeferAsync();
        UserTimezoneSettings? userTimezone = null;

        try
        {
            userTimezone = await _service.GetUserTimezoneSettingsAsync(interaction.User.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get user timezone");
            await interaction.FollowupAsync("Failed to get your timezone.");
        }

        if (userTimezone == null)
        {
            await interaction.FollowupAsync("You haven't set your timezone yet. Use `/set-timezone` to set it.");
            return;
        }

        var time = await _service.GetTimeForUser(interaction.User.Id, DateTimeOffset.UtcNow);
        await interaction.FollowupAsync($"The current time in your timezone is **{time:HH:mm:ss}**");
    }
}
