using Discord;
using Vereesa.Core.Infrastructure;
using Vereesa.Neon.Integrations;
using System.Web;
using Vereesa.Neon.Extensions;

namespace Vereesa.Neon.Services
{
    public class RaidSplitService : IBotService
    {
        private enum RaidRole
        {
            Tank,
            Healer,
            RangedDps,
            MeleeDps,
            Unknown
        }

        private IWarcraftLogsApi _warcraftLogs;

        public RaidSplitService(IWarcraftLogsApi warcraftLogs)
        {
            _warcraftLogs = warcraftLogs;
        }

        [OnCommand("!raid split")]
        [AsyncHandler]
        [WithArgument("requestedNumberOfGroups", 0)]
        [WithArgument("roles", 1)]
        public async Task SplitRaidEvenlyAsync(IMessage message, string requestedNumberOfGroups, string rolesInput)
        {
            var requestedGroupSize = 40;

            if (requestedNumberOfGroups.Contains("x"))
            {
                var groupSizeSplit = requestedNumberOfGroups.Split("x");
                requestedNumberOfGroups = groupSizeSplit[0].Trim();
                int.TryParse(groupSizeSplit[1], out requestedGroupSize);
            }

            if (!int.TryParse(requestedNumberOfGroups, out var numberOfGroups))
            {
                await message.Channel.SendMessageAsync(
                    "Please enter a desired number of groups to split the raid into."
                );

                return;
            }

            var includedRoles = GetIncludedRoles(rolesInput);
            var raidMembers = await GetLastRaidMembersAsync();
            var roleGroups = raidMembers
                .OrderBy(m => m.Guid)
                .GroupBy(m => MapSpec(m.Type, m.Specs.First()))
                .Where(grp => includedRoles.Contains(grp.Key));

            var groups = new int[numberOfGroups]
                .Select(c => new List<ReportCharacter>())
                .ToArray();

            foreach (var role in roleGroups)
            {
                for (var i = 0; i < role.Count(); i++)
                {
                    groups[i % numberOfGroups].Add(role.ElementAt(i));
                }

                groups = groups.OrderBy(g => g.Count).ToArray();
            }

            var embed = GenerateEmbed(groups, numberOfGroups, requestedGroupSize, message.Author);
            await message.Channel.SendMessageAsync(embed: embed);
        }

        private async Task<List<ReportCharacter>> GetLastRaidMembersAsync()
        {
            var lastRaid = (await _warcraftLogs.GetRaidReports()).First();
            var totalRaidDuration = lastRaid.End - lastRaid.Start;
            var windowStart = totalRaidDuration - (totalRaidDuration * 0.5); // last half
            var windowEnd = totalRaidDuration;

            return await _warcraftLogs.GetRaidComposition(lastRaid.Id, (long)windowStart, windowEnd);
        }

        private Embed GenerateEmbed(
            List<ReportCharacter>[] groups,
            int numberOfGroups,
            int requestedGroupSize,
            IUser requester
        )
        {
            var embed = new EmbedBuilder().WithTitle("Raid Split Result").WithColor(new Color(155, 89, 182));

            var export = "";
            var grpNum = 1;
            foreach (var group in groups)
            {
                var groupMembers = string.Join(
                    "\n",
                    group.Take(requestedGroupSize).Select(p => $"{GroupIcon(p.Type, p.Specs.First())} {p.Name}")
                );

                embed.AddField($"**Group {grpNum}**", groupMembers, true);

                if (grpNum > 1)
                {
                    export += "\n\n";
                }

                export += $"**Group {grpNum}**\n";
                export += groupMembers;

                grpNum++;
            }

            var exportLink =
                "https://vereesa.neon.gg/ert/?note=" + HttpUtility.UrlEncode(Compressor.Zip(ErtFormat(export)));

            embed
                .AddField(
                    "Summary",
                    $"I have split the raid into `{numberOfGroups}` groups of max `{requestedGroupSize}` members.\n"
                        + "You can also do `!raid split 2x2 melee-dps`, `!raid split 2 healer`, or `!raid split 3x3 ranged`. "
                        + "Try it out!\n\n"
                )
                .AddField("Export", $"Click [here]({exportLink}) to find a nice export to ERT.")
                .WithFooter(
                    new EmbedFooterBuilder().WithText(
                        $"Requested by {requester.Username}"
                            + $" - Today at {DateTimeExtensions.NowInCentralEuropeanTime().ToString("HH:mm")}"
                    )
                );

            return embed.Build();
        }

        private string ErtFormat(string input)
        {
            return input
                .Replace("💖", "{spell:6940}")
                .Replace("🛡", "{spell:193991}")
                .Replace("🏹", "{spell:259006}")
                .Replace("⚔", "{spell:138422}");
        }

        private HashSet<RaidRole> GetIncludedRoles(string rolesInput)
        {
            switch (rolesInput.ToLowerInvariant())
            {
                case "dps":
                    return new HashSet<RaidRole> { RaidRole.MeleeDps, RaidRole.RangedDps };
                case "melee":
                    return new HashSet<RaidRole> { RaidRole.MeleeDps, RaidRole.Tank };
                case "melee-dps":
                    return new HashSet<RaidRole> { RaidRole.MeleeDps };
                case "ranged":
                    return new HashSet<RaidRole> { RaidRole.Healer, RaidRole.RangedDps };
                case "ranged-dps":
                    return new HashSet<RaidRole> { RaidRole.RangedDps };
                case "healer":
                    return new HashSet<RaidRole> { RaidRole.Healer };
                case "tank":
                    return new HashSet<RaidRole> { RaidRole.Tank };
                default:
                    return new HashSet<RaidRole>
                    {
                        RaidRole.Healer,
                        RaidRole.Tank,
                        RaidRole.RangedDps,
                        RaidRole.MeleeDps
                    };
            }
        }

        private string GroupIcon(string className, Specialization specialization)
        {
            switch (MapSpec(className, specialization))
            {
                case RaidRole.RangedDps:
                    return "🏹";
                case RaidRole.MeleeDps:
                    return "⚔";
                case RaidRole.Healer:
                    return "💖";
                case RaidRole.Tank:
                    return "🛡";
                default:
                    return "❔";
            }
        }

        private RaidRole MapSpec(string className, Specialization specialization)
        {
            if (specialization.Role == "tank")
            {
                return RaidRole.Tank;
            }

            if (specialization.Role == "healer")
            {
                return RaidRole.Healer;
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
                    return RaidRole.MeleeDps;
                case "Frost":
                    return className == "Mage" ? RaidRole.RangedDps : RaidRole.MeleeDps;
                default:
                    return RaidRole.RangedDps;
            }
        }
    }
}
