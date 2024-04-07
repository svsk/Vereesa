using Discord;
using Vereesa.Neon.Integrations.Interfaces;
using Discord.Interactions;
using Vereesa.Core;
using System.ComponentModel;
using Vereesa.Neon.Data.Models;
using Vereesa.Neon.Data.Interfaces;
using Vereesa.Neon.Data.Models.Wowhead;
using Vereesa.Core.Infrastructure;
using Microsoft.Extensions.Logging;
using Vereesa.Neon.Helpers;

namespace Vereesa.Neon.Services;

public class TodayInWoWService : IBotModule
{
    private readonly IWowheadClient _wowhead;
    private readonly IRepository<ElementalStormSubscription> _subscriptionRepository;
    private readonly IRepository<GrandHuntSubscription> _huntSubscriptionRepository;
    private readonly IMessagingClient _messagingClient;
    private readonly ILogger<TodayInWoWService> _logger;

    public TodayInWoWService(
        IWowheadClient wowhead,
        IRepository<ElementalStormSubscription> subscriptionRepository,
        IRepository<GrandHuntSubscription> huntSubscriptionRepository,
        IMessagingClient messagingClient,
        ILogger<TodayInWoWService> logger
    )
    {
        _wowhead = wowhead;
        _subscriptionRepository = subscriptionRepository;
        _huntSubscriptionRepository = huntSubscriptionRepository;
        _messagingClient = messagingClient;
        _logger = logger;
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
        var wowZone = (WoWZone)zone;

        await interaction.DeferAsync(ephemeral: true);

        var existingSubscription = await GetHuntSubscription(interaction.User.Id, wowZone);
        if (existingSubscription != null)
        {
            await interaction.FollowupAsync(
                $"🤔 You are already subscribed to Grand Hunts in {WoWZoneHelper.GetName(wowZone)}.",
                ephemeral: true
            );
            return;
        }

        await AddHuntSubscription(interaction.User.Id, wowZone);

        await interaction.FollowupAsync(
            $"🪄 You will now be notified about Grand Hunts in {WoWZoneHelper.GetName(wowZone)}.",
            ephemeral: true
        );
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
        var wowZone = (WoWZone)zone;

        await interaction.DeferAsync(ephemeral: true);

        var subscription = await GetHuntSubscription(interaction.User.Id, wowZone);
        if (subscription == null)
        {
            await interaction.FollowupAsync(
                $"😅 You are not subscribed to Grand Hunts in {WoWZoneHelper.GetName(wowZone)} ",
                ephemeral: true
            );
            return;
        }

        await _huntSubscriptionRepository.DeleteAsync(subscription);
        await _huntSubscriptionRepository.SaveAsync();

        await interaction.FollowupAsync(
            $"✨ You will no longer be notified about Grand Hunts in {WoWZoneHelper.GetName(wowZone)}.",
            ephemeral: true
        );
    }

    [SlashCommand("elemental-storm", "Show current elemental storms")]
    public async Task GetCurrentElementalStorm(IDiscordInteraction interaction)
    {
        await interaction.DeferAsync();

        var currentStorms = await _wowhead.GetCurrentElementalStorms();

        if (currentStorms == null || !currentStorms.Any())
        {
            await interaction.FollowupAsync("💥 No elemental storms found.");
            return;
        }

        var embeds = currentStorms.Select(storm => CreateStormEmbed(storm).Build()).ToArray();

        await interaction.FollowupAsync(embeds: embeds);
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
        var stormType = (ElementalStormType)type;
        var wowZone = (WoWZone)zone;

        await interaction.DeferAsync(ephemeral: true);

        var existingSubscription = await GetStormSubscription(interaction.User.Id, stormType, wowZone);
        if (existingSubscription != null)
        {
            await interaction.FollowupAsync(
                $"🤔 You are already subscribed to {stormType}s in {WoWZoneHelper.GetName(wowZone)}.",
                ephemeral: true
            );
            return;
        }

        await AddSubscription(interaction.User.Id, stormType, wowZone);

        await interaction.FollowupAsync(
            $"🪄 You will now be notified about {stormType}s in {WoWZoneHelper.GetName(wowZone)}.",
            ephemeral: true
        );
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
        var stormType = (ElementalStormType)type;
        var wowZone = (WoWZone)zone;
        var zoneName = WoWZoneHelper.GetName(wowZone);

        await interaction.DeferAsync(ephemeral: true);

        var subscription = await GetStormSubscription(interaction.User.Id, stormType, wowZone);
        if (subscription == null)
        {
            await interaction.FollowupAsync(
                $"😅 You are not subscribed to {stormType}s in {zoneName} ",
                ephemeral: true
            );
            return;
        }

        await RemoveSubscription(subscription);

        await interaction.FollowupAsync(
            $"✨ You will no longer be notified about {stormType}s in {zoneName}.",
            ephemeral: true
        );
    }

    [OnInterval(Minutes = 10)]
    public async Task CheckForElementalStorms()
    {
        await NotifyStormSubscribers();
        await NotifyHuntSubscribers();
    }

    private async Task NotifyHuntSubscribers()
    {
        var currentGrandHunts = (await _wowhead.GetCurrentGrandHunts())?.ToList();

        if (currentGrandHunts == null || !currentGrandHunts.Any())
        {
            return;
        }

        var subscriptions = await _huntSubscriptionRepository.GetAllAsync();

        foreach (var grandHunt in currentGrandHunts)
        {
            foreach (var subscription in subscriptions)
            {
                var subscriptionIsForCurrentHunt = subscription.Zone == grandHunt.ZoneId;

                if (!subscriptionIsForCurrentHunt)
                {
                    continue;
                }

                var recentlyNotified =
                    subscription.LastNotifiedAt.HasValue && subscription.LastNotifiedAt.Value >= grandHunt.Time;

                if (recentlyNotified)
                {
                    continue;
                }

                var embed = CreateHuntEmbed(grandHunt).Build();

                await _messagingClient.SendMessageToUserByIdAsync(subscription.UserId, "", embed: embed);

                subscription.LastNotifiedAt = grandHunt.Time;
                await _huntSubscriptionRepository.AddOrEditAsync(subscription);
            }
        }
    }

    private async Task NotifyStormSubscribers()
    {
        var currentElementalStorms = (await _wowhead.GetCurrentElementalStorms())
            ?.Where(storm => storm.Status == "Active")
            .ToList();

        if (currentElementalStorms == null || !currentElementalStorms.Any())
        {
            return;
        }

        var subscriptions = await _subscriptionRepository.GetAllAsync();

        foreach (var storm in currentElementalStorms)
        {
            foreach (var subscription in subscriptions)
            {
                var subscriptionIsForCurrentStorm =
                    subscription.Type == storm.Type && subscription.Zone == storm.ZoneId;

                if (!subscriptionIsForCurrentStorm)
                {
                    continue;
                }

                var recentlyNotified =
                    subscription.LastNotifiedAt.HasValue && subscription.LastNotifiedAt.Value >= storm.StartingAt;

                if (recentlyNotified)
                {
                    continue;
                }

                var embed = CreateStormEmbed(storm).Build();

                await _messagingClient.SendMessageToUserByIdAsync(subscription.UserId, "", embed: embed);

                subscription.LastNotifiedAt = storm.StartingAt;
                await _subscriptionRepository.AddOrEditAsync(subscription);
            }
        }
    }

    private EmbedBuilder CreateHuntEmbed(GrandHunt grandHunt)
    {
        var builder = new EmbedBuilder()
            .WithTitle("Current Grand Hunt")
            .AddField("Zone", grandHunt.Zone ?? "Unknown", true)
            .WithColor(VereesaColors.VereesaPurple)
            .WithThumbnailUrl("https://wow.zamimg.com/images/wow/icons/large/inv_misc_coinbag10.jpg")
            .WithFooter($"Started at {grandHunt.StartedAt:HH:mm} (UTC)\nEnding at {grandHunt.EndingAt:HH:mm} (UTC)");

        return builder;
    }

    private EmbedBuilder CreateStormEmbed(ElementalStorm elementalStorm)
    {
        var startingOrStarted = elementalStorm.StartingAt > DateTimeOffset.UtcNow ? "Starting" : "Started";

        var builder = new EmbedBuilder()
            .WithTitle("Current Elemental Storm")
            .AddField("Type", elementalStorm.Type?.ToString() ?? "Unknown", true)
            .AddField("Zone", elementalStorm.Zone ?? "Unknown", true)
            .AddField("Status", elementalStorm.Status ?? "Unknown", true)
            .WithColor(VereesaColors.VereesaPurple)
            .WithFooter(
                $"{startingOrStarted} at {elementalStorm.StartingAt:HH:mm} (UTC)\nEnding at {elementalStorm.EndingAt:HH:mm} (UTC)"
            );

        if (elementalStorm.IconUrl != null)
        {
            builder.WithThumbnailUrl(elementalStorm.IconUrl);
        }

        return builder;
    }

    private async Task AddHuntSubscription(ulong userId, WoWZone wowZone)
    {
        var subscription = new GrandHuntSubscription
        {
            Id = Guid.NewGuid().ToString(),
            UserId = userId,
            Zone = wowZone
        };

        await _huntSubscriptionRepository.AddAsync(subscription);
        await _huntSubscriptionRepository.SaveAsync();

        _logger.LogWarning("User {UserId} subscribed to Grand Hunts in {Zone}", userId, WoWZoneHelper.GetName(wowZone));
    }

    private async Task AddSubscription(ulong userId, ElementalStormType stormType, WoWZone wowZone)
    {
        var subscription = new ElementalStormSubscription
        {
            Id = Guid.NewGuid().ToString(),
            UserId = userId,
            Type = stormType,
            Zone = wowZone
        };

        await _subscriptionRepository.AddAsync(subscription);
        await _subscriptionRepository.SaveAsync();

        _logger.LogWarning(
            "User {UserId} subscribed to {StormType}s in {Zone}",
            userId,
            stormType,
            WoWZoneHelper.GetName(wowZone)
        );
    }

    private async Task RemoveSubscription(ElementalStormSubscription subscription)
    {
        await _subscriptionRepository.DeleteAsync(subscription);
        await _subscriptionRepository.SaveAsync();

        _logger.LogWarning(
            "User {UserId} unsubscribed from {StormType}s in {Zone}",
            subscription.UserId,
            subscription.Type,
            WoWZoneHelper.GetName(subscription.Zone)
        );
    }

    private async Task<ElementalStormSubscription?> GetStormSubscription(
        ulong userId,
        ElementalStormType stormType,
        WoWZone wowZone
    )
    {
        return (await _subscriptionRepository.GetAllAsync()).FirstOrDefault(
            x => x.UserId == userId && x.Type == stormType && x.Zone == wowZone
        );
    }

    private async Task<GrandHuntSubscription?> GetHuntSubscription(ulong userId, WoWZone wowZone)
    {
        return (await _huntSubscriptionRepository.GetAllAsync()).FirstOrDefault(
            x => x.UserId == userId && x.Zone == wowZone
        );
    }
}
