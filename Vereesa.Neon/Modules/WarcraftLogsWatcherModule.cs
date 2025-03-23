using Discord;
using Discord.Interactions;
using Microsoft.Extensions.Logging;
using Vereesa.Core;
using Vereesa.Core.Infrastructure;
using Vereesa.Core.Models;
using Vereesa.Neon.Data.Interfaces;
using Vereesa.Neon.Helpers;
using Vereesa.Neon.Integrations;

namespace Vereesa.Neon.Modules;

public class WarcraftLogsWatcherModule : IBotModule
{
    private readonly IEventsClient _events;
    private readonly IWarcraftLogsApi _warcraftLogs;
    private readonly ISimpleStore _store;
    private readonly IMessagingClient _messagingClient;
    private readonly ILogger<WarcraftLogsWatcherModule> _logger;
    private readonly string _storeKey = "WarcraftLogsAnnounceChannel";

    public WarcraftLogsWatcherModule(
        IEventsClient eventsClient,
        IWarcraftLogsApi warcraftLogs,
        ISimpleStore store,
        IMessagingClient messagingClient,
        ILogger<WarcraftLogsWatcherModule> logger
    )
    {
        _events = eventsClient;
        _warcraftLogs = warcraftLogs;
        _store = store;
        _messagingClient = messagingClient;
        _logger = logger;
    }

    [OnInterval(Minutes = 10)]
    public Task HandleTenMinutesPassed()
    {
        return Task.WhenAll(AnnounceNewReports());
    }

    [SlashCommand(
        "set-report-announce-channel",
        "Sets the current channel to be the target for new WarcraftLog report announcements."
    )]
    [Authorize(WellknownRoleNames.GuildMaster)]
    public async Task SetReportAnnounceChannelAsync(IDiscordInteraction interaction)
    {
        var channelId = interaction.ChannelId;

        _store.Set(_storeKey, channelId);

        await interaction.RespondAsync(
            "This channel is now the target for new WarcraftLog report announcements.",
            ephemeral: true
        );
    }

    private async Task AnnounceNewReports()
    {
        var announcementChannel = GetAnnouncementChannel();
        if (announcementChannel == null)
        {
            _logger.LogWarning(
                "🔧 No announcement channel set for WarcraftLogsWatcherModule. Use /set-report-announce-channel to configure."
            );

            return;
        }

        _logger.LogInformation("📊 Checking for new reports to announce.");

        var reportsToAnnounce = await FindReportsToAnnounce();

        foreach (var report in reportsToAnnounce)
        {
            var embed = CreateEmbedForReport(report);
            await _messagingClient.SendMessageToChannelByIdAsync(announcementChannel.Id, embed: embed);
        }
    }

    private Embed CreateEmbedForReport(Report report)
    {
        // Start date formatted as "YYYY-MM-DD HH:mm UTC"
        var reportStartDate = DateTimeOffset.FromUnixTimeMilliseconds(report.Start).ToString("yyyy-MM-dd HH:mm UTC");
        var reportUrl = $"https://www.warcraftlogs.com/reports/{report.Id}";

        var builder = new EmbedBuilder()
            .WithColor(VereesaColors.VereesaPurple)
            .WithAuthor("New Logs Available @ WarcraftLogs", null, reportUrl)
            .WithThumbnailUrl($"https://assets.rpglogs.com/img/warcraft/zones/zone-{report.Zone}.png")
            .WithTitle(report.Title ?? $"Neon Logs - {reportStartDate}")
            .WithDescription(
                $"A new set of logs were detected in relation with the ongoing raid. [Click here]({reportUrl}) to view them."
            )
            .WithFooter($"By {report.Owner} @ {reportStartDate}");

        return builder.Build();
    }

    private async Task<List<Report>> FindReportsToAnnounce()
    {
        // In order not to spam WarcraftLogs with too many requests.
        var ongoingEvent = await GetOngoingGuildEvent();
        if (ongoingEvent == null)
        {
            _logger.LogInformation("📊 No ongoing guild events right now.");
            ClearKnownReports();
            return new();
        }

        // Reports that were started 15 minutes before, or after the event started are considered relevant.
        var reports = await GetRelevantReports(ongoingEvent.StartTime.AddMinutes(-15));

        var reportsWeDidNotKnowAbout = reports.Where(report => !IsKnownReport(report)).ToList();

        SetKnownReports(reports);

        return reportsWeDidNotKnowAbout;
    }

    private void SetKnownReports(List<Report> reports)
    {
        _store.Set("KnownReports", reports);
    }

    private bool IsKnownReport(Report report)
    {
        var knownReports = GetKnownReports();
        return knownReports.Any(r => r.Id == report.Id);
    }

    private List<Report> GetKnownReports()
    {
        return _store.Get<List<Report>>("KnownReports") ?? new();
    }

    private void ClearKnownReports()
    {
        _store.Remove("KnownReports");
    }

    private async Task<List<Report>> GetRelevantReports(DateTimeOffset eventStartTime)
    {
        var reports = await _warcraftLogs.GetRaidReports();

        var relevantReports = reports
            .Where(r => r.Start != default && DateTimeOffset.FromUnixTimeMilliseconds(r.Start) > eventStartTime)
            .ToList();

        return relevantReports;
    }

    private IChannel? GetAnnouncementChannel() =>
        _store.Get<ulong?>(_storeKey) is ulong channelId ? _messagingClient.GetChannelById(channelId) : null;

    private async Task<VereesaEvent?> GetOngoingGuildEvent()
    {
        var events = await _events.GetGuildEvents(WellknownGuilds.Neon);

        foreach (var ev in events)
        {
            _logger.LogInformation(ev.StartTime.ToString());
        }

        return events.FirstOrDefault(e => e.Status == VereesaEventStatus.Active);
    }
}
