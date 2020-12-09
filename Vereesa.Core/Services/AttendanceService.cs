using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using RestSharp;
using Vereesa.Core.Extensions;
using Vereesa.Core.Infrastructure;
using Vereesa.Data.Interfaces;

namespace Vereesa.Core.Services
{
	public class AttendanceService : BotServiceBase
	{
		private RestClient _restClient;
		private IRepository<RaidAttendance> _attendanceRepo;
		private IRepository<RaidAttendanceSummary> _attendanceSummaryRepo;
		private ILogger<AttendanceService> _logger;

		private ulong _officerChatId = 247439963329789953;//124446036637908995;
		private ulong _officerRoleId = 124251615489294337;

		private string _guildId = "1179"; // Neon's WarcraftLogs guild ID.
		private Dictionary<string, string> _raidIds = new Dictionary<string, string>
		{
			//{ "ny'alotha, the waking city", "24" },
			{ "castle nathria", "26" },
		};

		public AttendanceService(DiscordSocketClient discord, IJobScheduler jobScheduler,
			IRepository<RaidAttendance> attendanceRepo, IRepository<RaidAttendanceSummary> attendanceSummaryRepo,
			ILogger<AttendanceService> logger)
			: base(discord)
		{
			_restClient = new RestClient();
			_attendanceRepo = attendanceRepo;
			_attendanceSummaryRepo = attendanceSummaryRepo;
			_logger = logger;
			jobScheduler.EveryDayAtUtcNoon += UpdateAttendanceAsync;
		}

		[OnCommand("!updateattendance")]
		[Authorize("Guild Master")]
		public async Task ForceAttendanceUpdate(IMessage message) 
		{
			await UpdateAttendanceAsync();
		}

		private async Task UpdateAttendanceAsync()
		{
			var zoneId = GetRaidIdOrDefault();

			var attendance = GetAttendanceFromWarcraftLogs(zoneId);

			foreach (var raid in attendance)
			{
				if (!(await _attendanceRepo.ItemExistsAsync(raid.Id)))
				{
					await _attendanceRepo.AddAsync(raid);
				}
			}

			var storedAttendance = (await _attendanceRepo.GetAllAsync()).Where(att => att.ZoneId == zoneId).ToList();

			var attendanceSummary = GenerateAttendanceSummary(zoneId, storedAttendance);
			await _attendanceSummaryRepo.AddOrEditAsync(attendanceSummary);

			var rankChanges = await CalculateRankChangesAsync(zoneId, storedAttendance);
			await AnnounceRankChangesAsync(rankChanges);
		}

		private async Task AnnounceRankChangesAsync(List<string> rankChanges)
		{
			if (!rankChanges.Any())
			{
				return;
			}

			var rankChangeMessage = $"{_officerRoleId.MentionRole()} Based on attendance from the **last 10 raids**, the following rank changes should be made:\n";
			rankChangeMessage += rankChanges.Join("\n");
			await ((ISocketMessageChannel)Discord.GetChannel(_officerChatId)).SendMessageAsync(rankChangeMessage);
		}

		private async Task<List<string>> CalculateRankChangesAsync(string zoneId, List<RaidAttendance> storedAttendance)
		{
			var rankChanges = new List<string>();

			var tenRaidSnapshotSummary = GenerateAttendanceSummary(zoneId, storedAttendance
				.OrderByDescending(r => r.Timestamp).Take(10).ToList()
			);
			tenRaidSnapshotSummary.Id = $"tenraid-{tenRaidSnapshotSummary.Id}";

			var prvSnapshot = await _attendanceSummaryRepo.FindByIdAsync(tenRaidSnapshotSummary.Id);

			if (prvSnapshot != null && storedAttendance.Count >= 3)
			{
				var thresholds = new Dictionary<decimal, string>()
				{
					{ 50, "Raider" },
					{ 90, "Devoted Raider" },
				};

				foreach (var currentRanking in tenRaidSnapshotSummary.Rankings)
				{
					var previousRanking = prvSnapshot.Rankings.FirstOrDefault(c => c.CharacterName == currentRanking.CharacterName);
					var currentPct = decimal.Parse(currentRanking.AttendancePercentage);
					var previousPct = decimal.Parse(previousRanking?.AttendancePercentage ?? "0");

					foreach (var threshold in thresholds)
					{
						var thresholdPct = threshold.Key;
						var rankName = threshold.Value;

						if (previousPct >= thresholdPct && currentPct < thresholdPct)
						{
							rankChanges.Add($"{currentRanking.CharacterName} should be demoted from {rankName} ({currentRanking.AttendancePercentage}%).");
						}
						else if (previousPct < thresholdPct && currentPct >= thresholdPct)
						{
							rankChanges.Add($"{currentRanking.CharacterName} should be promoted to {rankName} ({currentRanking.AttendancePercentage}%).");
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
				foreach (var character in raid.Attendees)
				{
					if (!dict.ContainsKey(character))
					{
						dict.Add(character, 0);
					}

					dict[character]++;
				}
			}

			var summary = new RaidAttendanceSummary($"wcl-zone-{zoneId}");

			summary.Rankings = dict
				.OrderByDescending(k => k.Value)
				.Select(kv =>
				{
					var characterName = kv.Key;
					var attendancePercent = (kv.Value / (decimal)raids.Count * 100)
						.ToString("0.##", CultureInfo.InvariantCulture);

					return new RaidAttendanceRanking(characterName, attendancePercent);
				})
				.ToList();

			return summary;
		}

		[OnCommand("!attendance")]
		public async Task HandleMessageReceived(IMessage message)
		{
			var zoneId = GetRaidIdOrDefault(message.Content.Split(" ").Skip(1).Join(" "));

			var summary = await _attendanceSummaryRepo.FindByIdAsync($"wcl-zone-{zoneId}");

			var characterList = string.Join("\n", summary.Rankings
				.Select(ranking => $"{ranking.CharacterName}: {ranking.AttendancePercentage}%")
			);

			var truncated = false;
			if (characterList.Length > 1700)
			{
				characterList = characterList.Substring(0, characterList.Substring(0, 1500).LastIndexOf("\n"));
				truncated = true;
			}

			var zoneName = GetRaidName(zoneId);

			var attendanceReport = $"**Attendance for {zoneName}**\nUpdated daily at 12:00 UTC. Only raids logged to WarcraftLogs are included.\n\n{characterList}";

			attendanceReport = !truncated ? attendanceReport : $"{attendanceReport}\n\nSome entries have been truncated.";

			await message.Channel.SendMessageAsync(attendanceReport);
		}

		private string GetRaidIdOrDefault(string raid = null)
		{
			var defaultRaidId = _raidIds.Last().Value;

			if (string.IsNullOrWhiteSpace(raid))
			{
				return defaultRaidId;
			}

			var requestedRaid = raid.Trim().ToLowerInvariant();
			return _raidIds.ContainsKey(requestedRaid) ? _raidIds[requestedRaid] : defaultRaidId;
		}

		public List<RaidAttendance> GetAttendanceFromWarcraftLogs(string zoneId, int page = 1)
		{
			var request = new RestRequest($"https://www.warcraftlogs.com/guild/attendance-table/{_guildId}/0/{zoneId}?page={page}", Method.GET);

			var result = _restClient.Execute(request);

			var doc = new HtmlDocument();
			doc.LoadHtml(result.Content);

			var lastPageStr = doc.DocumentNode.SelectNodes("//*[contains(@class, \"page-item\") and position() = (last() - 1)]")?.FirstOrDefault()?.InnerText;
			var lastPage = int.Parse(lastPageStr ?? "1");

			var header = doc.DocumentNode.SelectNodes("//*[@id=\"attendance-table\"]/thead/th");
			var characterRows = doc.DocumentNode.SelectNodes("//*[@id=\"attendance-table\"]/tbody/tr")?.ToList() ?? new List<HtmlNode>();

			var raids = header.Skip(2).Select(node =>
			{
				var reportDate = node.NextSibling.NextSibling.InnerText
					.Split("new Date(").Last().Split(")").First();

				var timestamp = long.Parse(reportDate);
				var raidName = GetRaidName(zoneId);

				return new RaidAttendance($"{_guildId}-{zoneId}-{reportDate}", timestamp, raidName, zoneId);
			}).ToList();

			foreach (var characterRow in characterRows)
			{
				var rowValues = characterRow.SelectNodes(".//td");
				var characterName = rowValues.Skip(0).FirstOrDefault().InnerText.Replace("\n", string.Empty);
				var attendanceRecord = rowValues.Skip(2).ToList();

				for (var i = 0; i < attendanceRecord.Count; i++)
				{
					if (attendanceRecord[i].InnerText.Contains("1"))
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

		private string GetRaidName(string zoneId)
		{
			if (!_raidIds.ContainsValue(zoneId))
				return "Unknown Zone";

			return _raidIds.First(kv => kv.Value == zoneId).Key.ToTitleCase();
		}
	}

	public class RaidAttendanceSummary : IEntity
	{
		public RaidAttendanceSummary(string id)
		{
			this.Id = id;
		}

		public string Id { get; set; }
		public List<RaidAttendanceRanking> Rankings { get; set; }
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

	public class RaidAttendance : IEntity
	{
		public RaidAttendance(string id, long timestamp, string zoneName, string zoneId)
		{
			Id = id;
			Timestamp = timestamp;
			ZoneId = zoneId;
			ZoneName = zoneName;
			Attendees = new List<string>();
		}

		public string Id { get; set; }
		public long Timestamp { get; set; }
		public string ZoneName { get; set; }
		public string ZoneId { get; set; }
		public List<string> Attendees { get; set; }
	}


}