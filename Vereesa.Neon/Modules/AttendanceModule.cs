using Vereesa.Core;
using System.ComponentModel;
using Discord;
using Microsoft.Extensions.Logging;
using Vereesa.Core.Extensions;
using Vereesa.Core.Infrastructure;
using Vereesa.Neon.Services;
using Vereesa.Neon.Helpers;

namespace Vereesa.Neon.Modules;

public class AttendanceModule : IBotModule
{
    private readonly ILogger<AttendanceService> _logger;
    private IMessagingClient _messagingClient;
    private AttendanceService _attendanceService;

    public AttendanceModule(
        IMessagingClient messagingClient,
        AttendanceService attendanceService,
        IJobScheduler jobScheduler,
        ILogger<AttendanceService> logger
    )
    {
        _logger = logger;
        _messagingClient = messagingClient;

        _attendanceService = attendanceService;
        jobScheduler.EveryDayAtUtcNoon += TriggerPeriodicAttendanceUpdateAsync;
    }

    private async Task TriggerPeriodicAttendanceUpdateAsync()
    {
        var rankChanges = await _attendanceService.UpdateAttendanceAsync(false);
        if (rankChanges?.Any() != true)
        {
            return;
        }

        var rankChangeMessage =
            $"{WellknownRoles.Officer.MentionRole()} Based on attendance from the **last 10 raids**, the following rank changes should be made:\n";

        rankChangeMessage += rankChanges.Join("\n");

        await _messagingClient.SendMessageToChannelByIdAsync(WellknownChannels.OfficerChat, rankChangeMessage);
    }

    [OnCommand("!attendance update")]
    [Description("Updates attendance. Only available to Guild Master role.")]
    [Authorize("Guild Master")]
    [AsyncHandler]
    public async Task ForceAttendanceUpdate(IMessage message)
    {
        await message.Channel.SendMessageAsync("Updating attendance...");
        await _attendanceService.UpdateAttendanceAsync(true);
        await message.Channel.SendMessageAsync("✅ Attendance updated.");
    }

    [OnCommand("!attendance prune")]
    [Description("Prunes duplicate attendance. Only available to Guild Master role.")]
    [Authorize("Guild Master")]
    [AsyncHandler]
    public async Task PruneAttendance(IMessage message)
    {
        var raidIds = _attendanceService.GetRaidIds();

        var zoneId = (
            await _messagingClient.Prompt(
                message.Author,
                $"What zone ID? (Hint: {raidIds.Last().Value})",
                message.Channel
            )
        )?.Content;

        if (zoneId == null)
        {
            await message.Channel.SendMessageAsync("No zone ID provided.");
            return;
        }

        var raidsToPrune = await _attendanceService.GetRaidsWithMultipleReports(zoneId);
        if (!raidsToPrune.Any())
        {
            await message.Channel.SendMessageAsync("No raids with multiple reports found.");
            return;
        }

        foreach (var raid in raidsToPrune)
        {
            var raidsToMerge = _attendanceService.GetPruneState(raid);

            var response = await _messagingClient.Prompt(
                message.Author,
                $"Detected possible duplicate reports.\n\n{raidsToMerge}\n"
                    + "Pick an action: \n`merge`\n`pick <id>`\n`ignore`",
                message.Channel,
                60000
            );

            var strategy = response?.Content ?? "ignore";
            _attendanceService.PruneAttendanceForRaid(raid, strategy);
        }

        await message.Channel.SendMessageAsync("✅ Attendance pruned.");
    }

    [OnCommand("!attendance total")]
    [Description("Lists current attendance standing.")]
    public async Task HandleMessageReceived(IMessage message)
    {
        var zoneId = _attendanceService.GetRaidIdOrDefault(message.Content.Split(" ").Skip(1).Join(" "));
        var summary = await _attendanceService.GetRaidSummary(zoneId);

        var characterList = string.Join(
            "\n",
            summary.Rankings.Select(ranking => $"{ranking.CharacterName}: {ranking.AttendancePercentage}%")
        );

        var truncated = false;
        if (characterList.Length > 1700)
        {
            characterList = characterList.Substring(0, characterList.Substring(0, 1500).LastIndexOf("\n"));
            truncated = true;
        }

        var zoneName = _attendanceService.GetRaidName(zoneId);

        var attendanceReport =
            $"**Attendance for {zoneName}**\n"
            + $"Updated daily at 12:00 UTC. Only raids logged to WarcraftLogs are included.\n\n{characterList}";

        attendanceReport = !truncated ? attendanceReport : $"{attendanceReport}\n\nSome entries have been truncated.";

        await message.Channel.SendMessageAsync(attendanceReport);
    }

    [OnCommand("!attendance")]
    [Description("Lists attendance from last ten raids")]
    public async Task ListLatestAttendance(IMessage message)
    {
        var zoneId = _attendanceService.GetRaidIdOrDefault();
        var zoneName = _attendanceService.GetRaidName(zoneId);
        var summary = _attendanceService.GetSummary(zoneId);

        var characterList = string.Join(
            "\n",
            summary.Rankings.Select(ranking => $"{ranking.CharacterName}: {ranking.AttendancePercentage}%")
        );

        var truncated = false;
        if (characterList.Length > 1700)
        {
            characterList = characterList.Substring(0, characterList.Substring(0, 1500).LastIndexOf("\n"));
            truncated = true;
        }

        var attendanceReport =
            $"**Attendance for the last 10 raids in {zoneName}**\n"
            + $"Updated daily at 12:00 UTC. Only raids logged to WarcraftLogs are included.\n\n{characterList}";

        attendanceReport = !truncated ? attendanceReport : $"{attendanceReport}\n\nSome entries have been truncated.";

        await message.Channel.SendMessageAsync(attendanceReport);
    }
}
