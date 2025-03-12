using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using RestSharp;
using Vereesa.Core.Extensions;
using Vereesa.Neon.Data.Models.Attendance;

namespace Vereesa.Neon.Integrations;

public interface IWarcraftLogsScraper
{
    List<RaidAttendance> GetAttendanceFromWarcraftLogs(string zoneId, int page);
    string GetRaidIdOrDefault(string? raid = null);
    Dictionary<string, string> GetRaidIds();
    string GetRaidName(string zoneId);
}

public class WarcraftLogsScraper : IWarcraftLogsScraper
{
    private readonly RestClient _restClient;
    private readonly ILogger<WarcraftLogsScraper> _logger;
    private string _guildId = "1179"; // Neon's WarcraftLogs guild ID.

    private Dictionary<string, string> _raidIds = new Dictionary<string, string>
    {
        { "ny'alotha, the waking city", "24" },
        { "castle nathria", "26" },
        { "sanctum of domination", "28" },
        { "sepulcher of the first ones", "29" },
        { "vault of the incarnates", "31" },
    };

    public WarcraftLogsScraper(ILogger<WarcraftLogsScraper> logger)
    {
        _restClient = new RestClient();
        _logger = logger;
    }

    public Dictionary<string, string> GetRaidIds() => _raidIds;

    public string GetRaidIdOrDefault(string? raid = null)
    {
        var defaultRaidId = _raidIds.Last().Value;

        if (string.IsNullOrWhiteSpace(raid))
        {
            return defaultRaidId;
        }

        var requestedRaid = raid.Trim().ToLowerInvariant();
        return _raidIds.ContainsKey(requestedRaid) ? _raidIds[requestedRaid] : defaultRaidId;
    }

    public string GetRaidName(string zoneId)
    {
        if (!_raidIds.ContainsValue(zoneId))
            return "Unknown Zone";

        return _raidIds.First(kv => kv.Value == zoneId).Key.ToTitleCase();
    }

    public List<RaidAttendance> GetAttendanceFromWarcraftLogs(string zoneId, int page = 1)
    {
        var request = new RestRequest(
            $"https://www.warcraftlogs.com/guild/attendance-table/{_guildId}/0/{zoneId}?page={page}",
            Method.Get
        );

        var result = _restClient.Execute(request);

        var doc = new HtmlDocument();
        doc.LoadHtml(result.Content);

        var lastPageStr = doc
            .DocumentNode.SelectNodes("//*[contains(@class, \"page-item\") and position() = (last() - 1)]")
            ?.FirstOrDefault()
            ?.InnerText;
        var lastPage = int.Parse(lastPageStr ?? "1");

        var header = doc.DocumentNode.SelectNodes("//*[@id=\"attendance-table\"]/thead/th");
        var characterRows =
            doc.DocumentNode.SelectNodes("//*[@id=\"attendance-table\"]/tbody/tr")?.ToList() ?? new List<HtmlNode>();

        var raids = header
            .Skip(2)
            .Select(node =>
            {
                var reportDate = node.NextSibling.NextSibling.InnerText.Split("new Date(").Last().Split(")").First();

                var timestamp = long.Parse(reportDate);
                var raidName = GetRaidName(zoneId);
                var relativeLogUrl = node.ChildNodes[0].Attributes.First(att => att.Name == "href").Value;
                var logUrl = $"https://www.warcraftlogs.com{relativeLogUrl}";

                return new RaidAttendance($"{_guildId}-{zoneId}-{reportDate}", timestamp, raidName, zoneId, logUrl);
            })
            .ToList();

        foreach (var characterRow in characterRows)
        {
            var rowValues = characterRow.SelectNodes(".//td");
            if (rowValues == null)
            {
                _logger.LogWarning("Row values were null. Skipping row.");
                continue;
            }

            var characterName = rowValues.Skip(0).FirstOrDefault()?.InnerText.Replace("\n", string.Empty);
            var attendanceRecord = rowValues.Skip(2).ToList();

            if (characterName == null)
            {
                _logger.LogWarning("Character name was null. Skipping row.");
                continue;
            }

            for (var i = 0; i < attendanceRecord.Count; i++)
            {
                if (attendanceRecord[i].InnerText.Contains("1") || attendanceRecord[i].InnerText.Contains("2"))
                {
                    raids[i].Attendees.Add(characterName);
                }
            }
        }

        if (lastPage > page)
        {
            raids.AddRange(GetAttendanceFromWarcraftLogs(zoneId, page + 1));
        }

        return raids;
    }
}
