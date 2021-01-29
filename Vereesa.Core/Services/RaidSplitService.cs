using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Vereesa.Core.Infrastructure;
using System.Linq;
using Vereesa.Core.Integrations;

namespace Vereesa.Core.Services
{
	public class RaidSplitService : BotServiceBase
	{
		private IWarcraftLogsApi _warcraftLogs;

		public RaidSplitService(DiscordSocketClient discord, IWarcraftLogsApi warcraftLogs)
			: base(discord)
		{
			_warcraftLogs = warcraftLogs;
		}

		[OnCommand("!raid split")]
		[AsyncHandler]
		[WithArgument("requestedNumberOfGroups", 0)]
		public async Task SplitRaidEvenlyAsync(IMessage message, string requestedNumberOfGroups)
		{
			if (!int.TryParse(requestedNumberOfGroups, out var numberOfGroups))
			{
				await message.Channel
					.SendMessageAsync("Please enter a desired number of groups to split the raid into.");

				return;
			}

			var lastRaid = (await _warcraftLogs.GetRaidReports()).First();

			var totalRaidDuration = lastRaid.End - lastRaid.Start;
			var windowStart = totalRaidDuration - (totalRaidDuration * 0.5);
			var windowEnd = totalRaidDuration;
			var members = await _warcraftLogs.GetRaidComposition(lastRaid.Id, (long)windowStart, windowEnd);
			var roleGroups = members.OrderBy(m => m.Guid).GroupBy(m => MapSpec(m.Type, m.Specs.First()));

			var groups = new int[numberOfGroups].Select(c => new List<ReportCharacter>()).ToArray();

			var rangedDps = roleGroups.FirstOrDefault(rg => rg.Key == "ranged-dps")?.ToList()
				?? new List<ReportCharacter>();

			var meleeDps = roleGroups.FirstOrDefault(rg => rg.Key == "melee-dps")?.ToList()
				?? new List<ReportCharacter>();

			var tanks = roleGroups.FirstOrDefault(rg => rg.Key == "tank")?.ToList()
				?? new List<ReportCharacter>();

			var healers = roleGroups.FirstOrDefault(rg => rg.Key == "healer")?.ToList()
				?? new List<ReportCharacter>();

			for (var i = 0; i < rangedDps.Count; i++)
			{
				groups[i % numberOfGroups].Add(rangedDps.ElementAt(i));
			}

			groups = groups.OrderBy(g => g.Count).ToArray();

			for (var i = 0; i < meleeDps.Count; i++)
			{
				groups[i % numberOfGroups].Add(meleeDps.ElementAt(i));
			}

			groups = groups.OrderBy(g => g.Count).ToArray();

			for (var i = 0; i < tanks.Count; i++)
			{
				groups[i % numberOfGroups].Add(tanks.ElementAt(i));
			}

			groups = groups.OrderBy(g => g.Count).ToArray();

			for (var i = 0; i < healers.Count; i++)
			{
				groups[i % numberOfGroups].Add(healers.ElementAt(i));
			}

			var returnMessage = "";
			var grpNum = 1;
			foreach (var group in groups)
			{
				returnMessage += $"**Group {grpNum}**\n";
				returnMessage += string.Join("\n", group.Select(p => $"{GroupIcon(p.Type, p.Specs.First())} {p.Name}"));
				returnMessage += "\n\n";
				grpNum++;
			}

			await message.Channel.SendMessageAsync(returnMessage);
		}

		private string GroupIcon(string className, Specialization specialization)
		{
			switch (MapSpec(className, specialization))
			{
				case "ranged-dps":
					return "🏹";
				case "melee-dps":
					return "⚔";
				case "healer":
					return "💖";
				case "tank":
					return "🛡";
				default:
					return "❔";
			}
		}

		private string MapSpec(string className, Specialization specialization)
		{
			if (specialization.Role != "dps")
			{
				return specialization.Role;
			}

			switch (specialization.Spec)
			{
				case "Fury":
				case "Arms":
				case "Retribution":
				case "Surival":
				case "Assassination":
				case "Outlaw":
				case "Subtlety":
				case "Enhancement":
				case "Windwalker":
				case "Feral":
				case "Havoc":
				case "Unholy":
					return "melee-dps";
				case "Frost":
					return className == "Mage" ? "ranged-dps" : "melee-dps";
				default:
					return "ranged-dps";
			}
		}
	}
}
