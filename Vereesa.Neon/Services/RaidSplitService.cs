using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Web;
using Discord;
using Discord.Interactions;
using Vereesa.Core;
using Vereesa.Core.Infrastructure;
using Vereesa.Neon.Exceptions;
using Vereesa.Neon.Extensions;
using Vereesa.Neon.Helpers;
using Vereesa.Neon.Integrations;

namespace Vereesa.Neon.Services
{
    public class RaidSplitService : IBotModule
    {
        private enum RaidRole
        {
            Tank,
            Healer,
            RangedDps,
            MeleeDps,
            Unknown,
        }

        private static class RaidSplitTeamType
        {
            public const string All = "all";
            public const string DPS = "dps";
            public const string Melee = "melee";
            public const string MeleeDPS = "melee-dps";
            public const string Ranged = "ranged";
            public const string RangedDPS = "ranged-dps";
            public const string Healers = "healer";
            public const string Tanks = "tank";
        }

        private IWarcraftLogsApi _warcraftLogs;

        public RaidSplitService(IWarcraftLogsApi warcraftLogs)
        {
            _warcraftLogs = warcraftLogs;
        }

        [AsyncHandler]
        [SlashCommand("raid-split", "Split the raid up into teams")]
        public async Task ManageRaid(
            IDiscordInteraction interaction,
            [Description("Number of teams you want to split the raid into.")] long numberOfTeams,
            [Description("Roles you want to include in the split. If left empty, everyone will be included.")]
            [
                Choice("Everyone", RaidSplitTeamType.All),
                Choice("All Ranged", RaidSplitTeamType.Ranged),
                Choice("All Melee", RaidSplitTeamType.Melee),
                Choice("All DPS", RaidSplitTeamType.DPS),
                Choice("Melee DPS", RaidSplitTeamType.MeleeDPS),
                Choice("Ranged DPS", RaidSplitTeamType.RangedDPS),
                Choice("Healers", RaidSplitTeamType.Healers),
                Choice("Tanks", RaidSplitTeamType.Tanks)
            ]
            [Optional]
                string? roles,
            [Description("Ideal group size. If left empty, the raid will be split evenly between number of teams.")]
            [Optional]
                long idealGroupSize
        )
        {
            // Convert longs to ints
            if (numberOfTeams < 1 || numberOfTeams > 40)
            {
                await interaction.RespondAsync("Please enter a number of teams between 1 and 40.");
                return;
            }

            if (idealGroupSize < 0 || idealGroupSize > 40)
            {
                await interaction.RespondAsync("Please enter a group size between 1 and 40.");
                return;
            }

            await interaction.DeferAsync();

            try
            {
                var groups = await PerformSplit((int)numberOfTeams, roles ?? RaidSplitTeamType.All);

                var embed = GenerateEmbed(
                    groups,
                    (int)numberOfTeams,
                    idealGroupSize == 0 ? 40 : (int)idealGroupSize,
                    interaction.User
                );

                await interaction.FollowupAsync(embed: embed);
            }
            catch (NotFoundException ex)
            {
                await interaction.FollowupAsync(ex.Message);
            }
        }

        [OnCommand("!raid split")]
        [AsyncHandler]
        [WithArgument("requestedNumberOfGroups", 0)]
        [WithArgument("roles", 1)]
        public async Task SplitRaidEvenlyAsync(IMessage message, string requestedNumberOfGroups, string rolesInput)
        {
            int requestedGroupSize = 40;

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

            try
            {
                var groups = await PerformSplit(numberOfGroups, rolesInput);
                var embed = GenerateEmbed(groups, numberOfGroups, requestedGroupSize, message.Author);
                // Return the embed as a new message to the source channel.
                await message.Channel.SendMessageAsync(embed: embed);
            }
            catch (NotFoundException ex)
            {
                await message.Channel.SendMessageAsync(ex.Message);
            }
        }

        private async Task<List<ValidReportCharacter>[]> PerformSplit(int numberOfGroups, string teamRoles)
        {
            var includedRoles = GetIncludedRoles(teamRoles);

            // Fetches members from WarcraftLogs
            var raidMembers = await GetLastRaidMembersAsync();

            var roleGroups = raidMembers
                .GroupBy(m => MapSpec(m.ClassName, m.Specs.First()))
                .Where(grp => includedRoles.Contains(grp.Key))
                .ToList();

            var groups = new int[numberOfGroups]
                .Select(c => new List<ValidReportCharacter>())
                .ToArray();

            foreach (var role in roleGroups)
            {
                for (var i = 0; i < role.Count(); i++)
                {
                    groups[i % numberOfGroups].Add(role.ElementAt(i));
                }

                groups = groups.OrderBy(g => g.Count).ToArray();
            }

            return groups;
        }

        private record ValidReportCharacter(string Name, string ClassName, List<Specialization> Specs);

        private async Task<List<ValidReportCharacter>> GetLastRaidMembersAsync()
        {
            var lastRaid = (await _warcraftLogs.GetRaidReports()).FirstOrDefault();
            if (lastRaid?.Id == null)
            {
                throw new NotFoundException("Failed to retrieve any raids from WarcraftLogs.");
            }

            var totalRaidDuration = lastRaid.End - lastRaid.Start;
            var windowStart = totalRaidDuration - (totalRaidDuration * 0.5); // last half
            var windowEnd = totalRaidDuration;

            var raidComposition = await _warcraftLogs.GetRaidComposition(lastRaid.Id, (long)windowStart, windowEnd);

            return raidComposition
                .OrderBy(m => m.Guid)
                .Select(character =>
                    character.Name != null && character.Type != null && character.Specs?.Count > 0
                        ? new ValidReportCharacter(character.Name, character.Type, character.Specs)
                        : null
                )
                .OfType<ValidReportCharacter>()
                .ToList();
        }

        private Embed GenerateEmbed(
            List<ValidReportCharacter>[] groups,
            int numberOfGroups,
            int requestedGroupSize,
            IUser requester
        )
        {
            var embed = new EmbedBuilder().WithTitle("Raid Split Result").WithColor(VereesaColors.VereesaPurple);

            var export = "";
            var grpNum = 1;
            foreach (var group in groups)
            {
                var groupMembers = string.Join(
                    "\n",
                    group.Take(requestedGroupSize).Select(p => $"{GroupIcon(p.ClassName, p.Specs.First())} {p.Name}")
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
                case RaidSplitTeamType.DPS:
                    return new HashSet<RaidRole> { RaidRole.MeleeDps, RaidRole.RangedDps };
                case RaidSplitTeamType.Melee:
                    return new HashSet<RaidRole> { RaidRole.MeleeDps, RaidRole.Tank };
                case RaidSplitTeamType.MeleeDPS:
                    return new HashSet<RaidRole> { RaidRole.MeleeDps };
                case RaidSplitTeamType.Ranged:
                    return new HashSet<RaidRole> { RaidRole.Healer, RaidRole.RangedDps };
                case RaidSplitTeamType.RangedDPS:
                    return new HashSet<RaidRole> { RaidRole.RangedDps };
                case RaidSplitTeamType.Healers:
                    return new HashSet<RaidRole> { RaidRole.Healer };
                case RaidSplitTeamType.Tanks:
                    return new HashSet<RaidRole> { RaidRole.Tank };
                case RaidSplitTeamType.All:
                default:
                    return new HashSet<RaidRole>
                    {
                        RaidRole.Healer,
                        RaidRole.Tank,
                        RaidRole.RangedDps,
                        RaidRole.MeleeDps,
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
                case "Survival":
                    return RaidRole.MeleeDps;
                case "Frost":
                    return className == "Mage" ? RaidRole.RangedDps : RaidRole.MeleeDps;
                default:
                    return RaidRole.RangedDps;
            }
        }
    }
}
