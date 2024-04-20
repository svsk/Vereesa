using System.Globalization;
using Microsoft.Extensions.Logging;
using Vereesa.Neon.Data.Interfaces;
using NodaTime;
using Vereesa.Core.Extensions;
using Vereesa.Neon.Data.Models.Attendance;
using Vereesa.Neon.Integrations;

namespace Vereesa.Neon.Services;

public class AttendanceService
{
    private readonly IRepository<RaidAttendance> _attendanceRepo;
    private readonly IRepository<RaidAttendanceSummary> _attendanceSummaryRepo;
    private readonly IRepository<UsersCharacters> _userCharacters;
    private readonly IWarcraftLogsScraper _scraper;
    private readonly ILogger<AttendanceService> _logger;

    public AttendanceService(
        IRepository<RaidAttendance> attendanceRepo,
        IRepository<RaidAttendanceSummary> attendanceSummaryRepo,
        IRepository<UsersCharacters> userCharacters,
        IWarcraftLogsScraper warcraftLogsScraper,
        ILogger<AttendanceService> logger
    )
    {
        _attendanceRepo = attendanceRepo;
        _attendanceSummaryRepo = attendanceSummaryRepo;
        _userCharacters = userCharacters;
        _scraper = warcraftLogsScraper;
        _logger = logger;
    }

    public RaidAttendanceSummary GetSummary(string raidId) =>
        _attendanceSummaryRepo.FindById($"tenraid-wcl-zone-{raidId}");

    public async Task<List<string>?> UpdateAttendanceAsync(bool forceUpdate)
    {
        var zoneId = _scraper.GetRaidIdOrDefault();

        var attendance = GetAttendanceFromWarcraftLogs(zoneId);

        foreach (var raid in attendance)
        {
            if (!await _attendanceRepo.ItemExistsAsync(raid.Id) || forceUpdate)
            {
                await _attendanceRepo.AddOrEditAsync(raid);
            }
        }

        var storedAttendance = (await _attendanceRepo.GetAllAsync())
            .Where(att => att.ZoneId == zoneId && !att.Excluded)
            .ToList();

        var attendanceSummary = GenerateAttendanceSummary(zoneId, storedAttendance);
        await _attendanceSummaryRepo.AddOrEditAsync(attendanceSummary);

        var rankChanges = await CalculateRankChangesAsync(zoneId, storedAttendance);

        return rankChanges;
    }

    private async Task<List<string>> CalculateRankChangesAsync(string zoneId, List<RaidAttendance> storedAttendance)
    {
        var rankChanges = new List<string>();

        var tenRaidSnapshotSummary = GenerateAttendanceSummary(
            zoneId,
            storedAttendance.OrderByDescending(r => r.Timestamp).Take(10).ToList()
        );
        tenRaidSnapshotSummary.Id = $"tenraid-{tenRaidSnapshotSummary.Id}";

        var prvSnapshot = await _attendanceSummaryRepo.FindByIdAsync(tenRaidSnapshotSummary.Id);

        if (prvSnapshot != null && storedAttendance.Count >= 3)
        {
            var thresholds = new Dictionary<decimal, string>() { { 50, "Raider" }, { 90, "Devoted Raider" }, };

            if (tenRaidSnapshotSummary.Rankings == null)
            {
                throw new Exception("Rankings were null.");
            }

            foreach (var currentRanking in tenRaidSnapshotSummary.Rankings)
            {
                var previousRanking = prvSnapshot.Rankings?.FirstOrDefault(
                    c => c.CharacterName == currentRanking.CharacterName
                );

                var currentPct = decimal.Parse(
                    currentRanking.AttendancePercentage,
                    NumberStyles.Any,
                    CultureInfo.InvariantCulture
                );
                var previousPct = decimal.Parse(
                    previousRanking?.AttendancePercentage ?? "0",
                    NumberStyles.Any,
                    CultureInfo.InvariantCulture
                );

                foreach (var threshold in thresholds)
                {
                    var thresholdPct = threshold.Key;
                    var rankName = threshold.Value;

                    if (previousPct >= thresholdPct && currentPct < thresholdPct)
                    {
                        rankChanges.Add(
                            $"{currentRanking.CharacterName} should be demoted from {rankName} ({currentRanking.AttendancePercentage}%)."
                        );
                    }
                    else if (previousPct < thresholdPct && currentPct >= thresholdPct)
                    {
                        rankChanges.Add(
                            $"{currentRanking.CharacterName} should be promoted to {rankName} ({currentRanking.AttendancePercentage}%)."
                        );
                    }
                }
            }
        }

        await _attendanceSummaryRepo.AddOrEditAsync(tenRaidSnapshotSummary);

        return rankChanges;
    }

    private RaidAttendanceSummary GenerateAttendanceSummary(string zoneId, List<RaidAttendance> raids)
    {
        var dict = new Dictionary<string, decimal>();

        foreach (var raid in raids)
        {
            var mappedAttendees = raid.Attendees
                .Select((character) => CharacterToPerson(character))
                .Distinct()
                .ToList();

            foreach (var character in mappedAttendees)
            {
                if (!dict.ContainsKey(character))
                {
                    dict.Add(character, 0);
                }

                dict[character]++;
            }
        }

        var summary = new RaidAttendanceSummary($"wcl-zone-{zoneId}");

        summary.Rankings = dict.OrderByDescending(k => k.Value)
            .Select(kv =>
            {
                var characterName = kv.Key;
                var attendancePercent = (kv.Value / (decimal)raids.Count * 100).ToString(
                    "0.##",
                    CultureInfo.InvariantCulture
                );

                return new RaidAttendanceRanking(characterName, attendancePercent);
            })
            .ToList();

        return summary;
    }

    private string CharacterToPerson(string character)
    {
        var users = _userCharacters.FindById(CharacterService.BlobContainer);
        var characterOwner = users.CharacterMap.FirstOrDefault(
            cm => cm.Value.Any(c => c.StartsWith($"{character}-", StringComparison.CurrentCultureIgnoreCase))
        );

        return characterOwner.Key != 0 ? characterOwner.Key.MentionPerson() : character;
    }

    public List<RaidAttendance> GetAttendanceFromWarcraftLogs(string zoneId, int page = 1) =>
        _scraper.GetAttendanceFromWarcraftLogs(zoneId, page);

    public async Task<List<IGrouping<string, RaidAttendance>>> GetRaidsWithMultipleReports(string zoneId)
    {
        var attendanceReports = (await _attendanceRepo.GetAllAsync()).Where(
            att => att.ZoneId == zoneId && !att.Excluded
        );

        var raidGroupings = attendanceReports
            .GroupBy(
                r => Instant.FromUnixTimeMilliseconds(r.Timestamp).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
            )
            .ToList();

        return raidGroupings.Where(r => r.Count() > 1).ToList();
    }

    public void PruneAttendanceForRaid(IGrouping<string, RaidAttendance> raidReports, string strategy)
    {
        if (strategy == "merge")
        {
            var recordToKeep = raidReports.First();
            foreach (var recordToExclude in raidReports.Skip(1))
            {
                recordToKeep.Attendees = recordToKeep.Attendees.Concat(recordToExclude.Attendees).Distinct().ToList();

                recordToExclude.Excluded = true;
                _attendanceRepo.AddOrEdit(recordToExclude);
            }

            _attendanceRepo.AddOrEdit(recordToKeep);
        }
        else if (strategy.StartsWith("pick ") == true)
        {
            var idx = int.Parse(strategy.Replace("pick ", "").Trim()) - 1;
            var recordToKeep = raidReports.ElementAt(idx);

            foreach (var recordToExclude in raidReports.Where(r => r.Id != recordToKeep.Id))
            {
                recordToExclude.Excluded = true;
                _attendanceRepo.AddOrEdit(recordToExclude);
            }
        }
    }

    public async Task<RaidAttendanceSummary> GetRaidSummary(string zoneId)
    {
        return await _attendanceSummaryRepo.FindByIdAsync($"wcl-zone-{zoneId}");
    }

    public string GetPruneState(IGrouping<string, RaidAttendance> raids)
    {
        return string.Join(
            "\n",
            raids.Select((r, idx) => $"[{idx + 1}] {raids.Key} - {r.LogUrl} - {r.Attendees.Count} attendees")
        );
    }
}

public class RaidAttendanceSummary : IEntity
{
    public RaidAttendanceSummary(string id)
    {
        this.Id = id;
    }

    public string Id { get; set; }
    public List<RaidAttendanceRanking>? Rankings { get; set; }
}

public class RaidAttendanceRanking
{
    public RaidAttendanceRanking(string characterName, string attendancePercentage)
    {
        CharacterName = characterName;
        AttendancePercentage = attendancePercentage;
    }

    public string CharacterName { get; set; }
    public string AttendancePercentage { get; set; }
}
