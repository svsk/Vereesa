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

public class RadiantEchoesModule : IBotModule
{
    private readonly TodayInWoWService _todayInWoWService;
    private readonly TimeZoneService _timeZoneService;
    private readonly IMessagingClient _messagingClient;

    public RadiantEchoesModule(
        TodayInWoWService todayInWoWService,
        TimeZoneService timeZoneService,
        IMessagingClient messagingClient
    )
    {
        _todayInWoWService = todayInWoWService;
        _timeZoneService = timeZoneService;
        _messagingClient = messagingClient;
    }

    [OnInterval(Minutes = 10)]
    public async Task ProcessSubscriptions()
    {
        await Task.WhenAll(ProcessEchoSubscription());
    }

    private async Task ProcessEchoSubscription()
    {
        var dueSubscriptions = await _todayInWoWService.GetDueRadiantEchoesSubscriptions();
        var userSubscriptionGroups = dueSubscriptions.GroupBy(sub => sub.Subscription.UserId);
        var userIdsToNotify = userSubscriptionGroups.Select(subs => subs.Key);
        var userTimezones = await _timeZoneService.GetTimeZonesForUserIds(userIdsToNotify, "UTC");

        foreach (var user in userSubscriptionGroups)
        {
            var userId = user.Key;
            var userSubscriptions = user.ToList();

            var embeds = userSubscriptions
                .Select(sub => CreateRadiantEchoesEmbed(sub.EchoesEvent, userTimezones[userId]).Build())
                .ToArray();

            await _messagingClient.SendMessageToUserByIdAsync(userId, "", embeds: embeds);

            _ = Task.WhenAll(
                userSubscriptions.Select(
                    sub =>
                        _todayInWoWService.SetRadiantEchoesSubscriptionNotified(
                            sub.Subscription,
                            sub.EchoesEvent.StartedAt
                        )
                )
            );
        }
    }

    private EmbedBuilder CreateRadiantEchoesEmbed(RadiantEchoesEvent echoesEvent, string withTimezone = "UTC")
    {
        var localeStartTime = TimeZoneInfo.ConvertTime(
            echoesEvent.StartedAt,
            TimeZoneInfo.FindSystemTimeZoneById(withTimezone)
        );

        var endingAt = "";
        if (echoesEvent.EndingAt != null)
        {
            var localeEndTime = TimeZoneInfo.ConvertTime(
                echoesEvent.EndingAt.Value,
                TimeZoneInfo.FindSystemTimeZoneById(withTimezone)
            );

            var endingOrEnded = localeEndTime < DateTimeOffset.Now ? "Ended" : "Ending";
            endingAt = $"\n{endingOrEnded} at {localeEndTime:HH:mm} ({withTimezone})";
        }

        var startingOrStarted = localeStartTime < DateTimeOffset.Now ? "Started" : "Starting";

        var builder = new EmbedBuilder()
            .WithTitle("💡 Radiant Echoes Event 🔊")
            .AddField("Zone", echoesEvent.Zone ?? "Unknown", true)
            .WithColor(VereesaColors.VereesaPurple)
            .WithThumbnailUrl("https://wow.zamimg.com/images/wow/icons/medium/spell_azerite_essence11.jpg")
            .WithFooter($"{startingOrStarted} at {localeStartTime:HH:mm} ({withTimezone}){endingAt}");

        return builder;
    }

    [SlashCommand("subscribe-to-echoes", "Subscribe to Radiant Echoes")]
    public async Task SubscribeToRadiantEchoes(
        IDiscordInteraction interaction,
        [Description("The zone to subscribe to")]
        [
            Choice("Dustwallow Marsh", (int)WoWZone.DustwallowMarsh),
            Choice("Searing Gorge", (int)WoWZone.SearingGorge),
            Choice("Dragonblight", (int)WoWZone.Dragonblight),
        ]
            long zone
    )
    {
        await interaction.DeferAsync(ephemeral: true);

        var wowZone = (WoWZone)zone;

        try
        {
            await _todayInWoWService.AddRadiantEchoesSubscription(interaction.User.Id, wowZone);

            await interaction.FollowupAsync(
                $"🪄 You will now be notified about Radiant Echo events in {WoWZoneHelper.GetName(wowZone)}.",
                ephemeral: true
            );
        }
        catch (AlreadySubscribedException)
        {
            await interaction.FollowupAsync(
                $"🤔 You are already subscribed to Radiant Echo events in {WoWZoneHelper.GetName(wowZone)}.",
                ephemeral: true
            );
        }
    }

    [SlashCommand("unsubscribe-from-echoes", "Unsubscribes from Radiant Echoes in a specific zone.")]
    public async Task UnsubscribeFromHunt(
        IDiscordInteraction interaction,
        [Description("The zone to unsubscribe from.")]
        [
            Choice("Dustwallow Marsh", (int)WoWZone.DustwallowMarsh),
            Choice("Searing Gorge", (int)WoWZone.SearingGorge),
            Choice("Dragonblight", (int)WoWZone.Dragonblight),
        ]
            long zone
    )
    {
        await interaction.DeferAsync(ephemeral: true);
        var wowZone = (WoWZone)zone;

        try
        {
            await _todayInWoWService.RemoveRadiantEchoesSubscription(interaction.User.Id, wowZone);

            await interaction.FollowupAsync(
                $"✨ You will no longer be notified about Radiant Echo events in {WoWZoneHelper.GetName(wowZone)}.",
                ephemeral: true
            );
        }
        catch (NotSubscribedException)
        {
            await interaction.FollowupAsync(
                $"😅 You are not subscribed to Radiant Echo events in {WoWZoneHelper.GetName(wowZone)} ",
                ephemeral: true
            );
        }
    }
}
