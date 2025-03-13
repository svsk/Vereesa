using System.ComponentModel;
using Discord;
using Discord.Interactions;
using Vereesa.Core;
using Vereesa.Core.Infrastructure;
using Vereesa.Neon.Data.Models;
using Vereesa.Neon.Data.Models.Wowhead;
using Vereesa.Neon.Exceptions;
using Vereesa.Neon.Helpers;
using Vereesa.Neon.Services;

namespace Vereesa.Neon.Modules;

public class TodayInWoWModule : IBotModule
{
    private readonly TodayInWoWService _todayInWoWService;
    private readonly TimeZoneService _timeZoneService;
    private readonly IMessagingClient _messagingClient;

    public TodayInWoWModule(
        TodayInWoWService todayInWoWService,
        TimeZoneService timeZoneService,
        IMessagingClient messagingClient
    )
    {
        _todayInWoWService = todayInWoWService;
        _timeZoneService = timeZoneService;
        _messagingClient = messagingClient;
    }

    [SlashCommand("subscribe-to-hunt", "Subscribes to a Grand Hunt in a specific zone.")]
    public async Task SubscribeToHunt(
        IDiscordInteraction interaction,
        [Description("The zone to subscribe to.")]
        [
            Choice("The Waking Shores", (int)WoWZone.TheWakingShores),
            Choice("Ohn'ahran Plains", (int)WoWZone.OhnAhranPlains),
            Choice("The Azure Span", (int)WoWZone.TheAzureSpan),
            Choice("Thaldraszus", (int)WoWZone.Thaldraszus)
        ]
            long zone
    )
    {
        await interaction.DeferAsync(ephemeral: true);

        var wowZone = (WoWZone)zone;

        try
        {
            await _todayInWoWService.AddHuntSubscription(interaction.User.Id, wowZone);

            await interaction.FollowupAsync(
                $"🪄 You will now be notified about Grand Hunts in {WoWZoneHelper.GetName(wowZone)}.",
                ephemeral: true
            );
        }
        catch (AlreadySubscribedException)
        {
            await interaction.FollowupAsync(
                $"🤔 You are already subscribed to Grand Hunts in {WoWZoneHelper.GetName(wowZone)}.",
                ephemeral: true
            );
        }
    }

    [SlashCommand("unsubscribe-from-hunt", "Unsubscribes from Grand Hunts in a specific zone.")]
    public async Task UnsubscribeFromHunt(
        IDiscordInteraction interaction,
        [Description("The zone to unsubscribe from.")]
        [
            Choice("The Waking Shores", (int)WoWZone.TheWakingShores),
            Choice("Ohn'ahran Plains", (int)WoWZone.OhnAhranPlains),
            Choice("The Azure Span", (int)WoWZone.TheAzureSpan),
            Choice("Thaldraszus", (int)WoWZone.Thaldraszus)
        ]
            long zone
    )
    {
        await interaction.DeferAsync(ephemeral: true);
        var wowZone = (WoWZone)zone;

        try
        {
            await _todayInWoWService.RemoveHuntSubscription(interaction.User.Id, wowZone);

            await interaction.FollowupAsync(
                $"✨ You will no longer be notified about Grand Hunts in {WoWZoneHelper.GetName(wowZone)}.",
                ephemeral: true
            );
        }
        catch (NotSubscribedException)
        {
            await interaction.FollowupAsync(
                $"😅 You are not subscribed to Grand Hunts in {WoWZoneHelper.GetName(wowZone)} ",
                ephemeral: true
            );
        }
    }

    [SlashCommand("elemental-storm", "Show current elemental storms")]
    public async Task GetCurrentElementalStorm(IDiscordInteraction interaction)
    {
        await interaction.DeferAsync();

        try
        {
            var getUserTimezone = _timeZoneService.GetUserTimeZone(interaction.User.Id);
            var getCurrentStorms = _todayInWoWService.GetCurrentElementalStorms();

            await Task.WhenAll(getUserTimezone, getCurrentStorms);

            var userTimezone = getUserTimezone.Result;
            var currentStorms = getCurrentStorms.Result;

            var embeds = currentStorms.Select(storm => CreateStormEmbed(storm, userTimezone).Build()).ToArray();

            await interaction.FollowupAsync(embeds: embeds);
        }
        catch (NotFoundException)
        {
            await interaction.FollowupAsync("💥 No elemental storms found.");
        }
    }

    [SlashCommand("subscribe-to-storm", "Subscribe to elemental storms")]
    public async Task SubscribeToStorm(
        IDiscordInteraction interaction,
        [Description("The type of storm to subscribe to")]
        [
            Choice("Thunderstorms", (int)ElementalStormType.Thunderstorm),
            Choice("Sandstorms", (int)ElementalStormType.Sandstorm),
            Choice("Firestorms", (int)ElementalStormType.Firestorm),
            Choice("Snowstorms", (int)ElementalStormType.Snowstorm)
        ]
            long type,
        [Description("The zone to subscribe to")]
        [
            Choice("The Waking Shores", (int)WoWZone.TheWakingShores),
            Choice("Ohn'ahran Plains", (int)WoWZone.OhnAhranPlains),
            Choice("The Azure Span", (int)WoWZone.TheAzureSpan),
            Choice("Thaldraszus", (int)WoWZone.Thaldraszus)
        ]
            long zone
    )
    {
        await interaction.DeferAsync(ephemeral: true);

        var stormType = (ElementalStormType)type;
        var wowZone = (WoWZone)zone;

        try
        {
            await _todayInWoWService.AddStormSubscription(interaction.User.Id, stormType, wowZone);

            await interaction.FollowupAsync(
                $"🪄 You will now be notified about {stormType}s in {WoWZoneHelper.GetName(wowZone)}.",
                ephemeral: true
            );
        }
        catch (AlreadySubscribedException)
        {
            await interaction.FollowupAsync(
                $"🤔 You are already subscribed to {stormType}s in {WoWZoneHelper.GetName(wowZone)}.",
                ephemeral: true
            );
        }
    }

    [SlashCommand("unsubscribe-from-storm", "Unsubscribe from elemental storms")]
    public async Task UnsubscribeFromStorm(
        IDiscordInteraction interaction,
        [Description("The type of storm to unsubscribe from")]
        [
            Choice("Thunderstorms", (int)ElementalStormType.Thunderstorm),
            Choice("Sandstorms", (int)ElementalStormType.Sandstorm),
            Choice("Firestorms", (int)ElementalStormType.Firestorm),
            Choice("Snowstorms", (int)ElementalStormType.Snowstorm)
        ]
            long type,
        [Description("The zone to unsubscribe from")]
        [
            Choice("The Waking Shores", (int)WoWZone.TheWakingShores),
            Choice("Ohn'ahran Plains", (int)WoWZone.OhnAhranPlains),
            Choice("The Azure Span", (int)WoWZone.TheAzureSpan),
            Choice("Thaldraszus", (int)WoWZone.Thaldraszus)
        ]
            long zone
    )
    {
        await interaction.DeferAsync(ephemeral: true);

        var stormType = (ElementalStormType)type;
        var wowZone = (WoWZone)zone;
        var zoneName = WoWZoneHelper.GetName(wowZone);

        try
        {
            await _todayInWoWService.RemoveStormSubscription(interaction.User.Id, stormType, wowZone);

            await interaction.FollowupAsync(
                $"✨ You will no longer be notified about {stormType}s in {zoneName}.",
                ephemeral: true
            );
        }
        catch (NotSubscribedException)
        {
            await interaction.FollowupAsync(
                $"😅 You are not subscribed to {stormType}s in {zoneName} ",
                ephemeral: true
            );
        }
    }

    [OnInterval(Minutes = 10)]
    public async Task ProcessSubscriptions()
    {
        await Task.WhenAll(ProcessStormSubscriptions(), ProcessHuntSubscriptions());
    }

    private async Task ProcessStormSubscriptions()
    {
        var dueSubscriptions = await _todayInWoWService.GetDueStormSubscriptions();
        var userSubscriptionGroups = dueSubscriptions.GroupBy(sub => sub.Subscription.UserId);
        var userIdsToNotify = userSubscriptionGroups.Select(subs => subs.Key);
        var userTimezones = await _timeZoneService.GetTimeZonesForUserIds(userIdsToNotify, "UTC");

        foreach (var user in userSubscriptionGroups)
        {
            var userId = user.Key;
            var userSubscriptions = user.ToList();

            var embeds = userSubscriptions
                .Select(sub => CreateStormEmbed(sub.Storm, userTimezones[userId]).Build())
                .ToArray();

            await _messagingClient.SendMessageToUserByIdAsync(userId, "", embeds: embeds);

            _ = Task.WhenAll(
                userSubscriptions.Select(sub =>
                    _todayInWoWService.SetSubscriptionNotified(sub.Subscription, sub.Storm.StartingAt)
                )
            );
        }
    }

    private async Task ProcessHuntSubscriptions()
    {
        var dueSubscriptions = await _todayInWoWService.GetDueHuntSubscriptions();
        var userSubscriptionGroups = dueSubscriptions.GroupBy(sub => sub.Subscription.UserId);
        var userIdsToNotify = userSubscriptionGroups.Select(subs => subs.Key);
        var userTimezones = await _timeZoneService.GetTimeZonesForUserIds(userIdsToNotify, "UTC");

        foreach (var user in userSubscriptionGroups)
        {
            var userId = user.Key;
            var userSubscriptions = user.ToList();

            var embeds = userSubscriptions
                .Select(sub => CreateHuntEmbed(sub.Hunt, userTimezones[userId]).Build())
                .ToArray();

            await _messagingClient.SendMessageToUserByIdAsync(userId, "", embeds: embeds);

            _ = Task.WhenAll(
                userSubscriptions.Select(sub =>
                    _todayInWoWService.SetHuntSubscriptionNotified(sub.Subscription, sub.Hunt.StartedAt)
                )
            );
        }
    }

    private EmbedBuilder CreateHuntEmbed(GrandHunt grandHunt, string withTimezone = "UTC")
    {
        var localeStartTime = TimeZoneInfo.ConvertTime(
            grandHunt.StartedAt,
            TimeZoneInfo.FindSystemTimeZoneById(withTimezone)
        );

        var localeEndTime = TimeZoneInfo.ConvertTime(
            grandHunt.EndingAt,
            TimeZoneInfo.FindSystemTimeZoneById(withTimezone)
        );

        var builder = new EmbedBuilder()
            .WithTitle("Current Grand Hunt")
            .AddField("Zone", grandHunt.Zone ?? "Unknown", true)
            .WithColor(VereesaColors.VereesaPurple)
            .WithThumbnailUrl("https://wow.zamimg.com/images/wow/icons/large/inv_misc_coinbag10.jpg")
            .WithFooter($"Started at {localeStartTime:HH:mm} (UTC)\nEnding at {localeEndTime:HH:mm} (UTC)");

        return builder;
    }

    private EmbedBuilder CreateStormEmbed(ElementalStorm elementalStorm, string withTimezone = "UTC")
    {
        var startingOrStarted = elementalStorm.StartingAt > DateTimeOffset.UtcNow ? "Starting" : "Started";

        var localeStartTime = TimeZoneInfo.ConvertTime(
            elementalStorm.StartingAt,
            TimeZoneInfo.FindSystemTimeZoneById(withTimezone)
        );

        var localeEndTime = TimeZoneInfo.ConvertTime(
            elementalStorm.EndingAt,
            TimeZoneInfo.FindSystemTimeZoneById(withTimezone)
        );

        var builder = new EmbedBuilder()
            .WithTitle("Current Elemental Storm")
            .AddField("Type", elementalStorm.Type?.ToString() ?? "Unknown", true)
            .AddField("Zone", elementalStorm.Zone ?? "Unknown", true)
            .AddField("Status", elementalStorm.Status ?? "Unknown", true)
            .WithColor(VereesaColors.VereesaPurple)
            .WithFooter(
                $"{startingOrStarted} at {localeStartTime:HH:mm} ({withTimezone})\nEnding at {localeEndTime:HH:mm} ({withTimezone})"
            );

        if (elementalStorm.IconUrl != null)
        {
            builder.WithThumbnailUrl(elementalStorm.IconUrl);
        }

        return builder;
    }
}
