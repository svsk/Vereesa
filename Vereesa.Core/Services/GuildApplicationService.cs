using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Vereesa.Core.Configuration;
using Vereesa.Core.Extensions;
using Vereesa.Core.Helpers;
using Vereesa.Data.Models.EventHub;
using Vereesa.Data.Models.NeonApi;

namespace Vereesa.Core.Services
{
    public class GuildApplicationService
    {
        private NeonApiService _neonApiService;
        private DiscordSocketClient _discord;
        private BattleNetApiService _battleNetApi;
        private GuildApplicationSettings _settings;
        private Dictionary<int, Application> _cachedApplications;

        public GuildApplicationService(NeonApiService neonApiService, DiscordSocketClient discord, GuildApplicationSettings settings, BattleNetApiService battleNetApi)
        {
            _discord = discord;
            _battleNetApi = battleNetApi;
            _settings = settings;
            _neonApiService = neonApiService;

            Start();
        }

        private void Start()
        {
            TimerHelpers.SetTimeout(() =>
            {
                var applications = _neonApiService.GetApplications();

                if (applications != null)
                {
                    CheckForNewApplications(applications).GetAwaiter().GetResult();
                }
            }, 30000, true, true);
        }

        private async Task CheckForNewApplications(IEnumerable<Application> applications)
        {
            Console.WriteLine($"Checking for new apps ({_cachedApplications?.Keys.Count ?? 0} cached)...");

            var isFirstRun = _cachedApplications == null;

            if (isFirstRun)
            {
                _cachedApplications = new Dictionary<int, Application>();
            }

            foreach (var application in applications)
            {
                if (!_cachedApplications.ContainsKey(application.Id) && !isFirstRun)
                {
                    Console.WriteLine($"Found new app! Announcing!");
                    await AnnounceNewApplication(application);
                }

                _cachedApplications[application.Id] = application;
            }

            Console.WriteLine($"Finished checking for new apps. Now {_cachedApplications.Keys.Count} cached.");
        }

        private async Task AnnounceNewApplication(Application application)
        {
            var notificationChannel = _discord.Guilds.FirstOrDefault()?.Channels.FirstOrDefault(c => c.Name == _settings.NotificationMessageChannelName) as ISocketMessageChannel;
            if (notificationChannel != null)
            {
                try
                {
                    //await notificationChannel.SendMessageAsync(string.Format(_settings.MessageToSendOnNewLine, fields));
                    var applicationEmbed = GetApplicationEmbed(application).Build();
                    await notificationChannel.SendMessageAsync("", false, applicationEmbed);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }
        }

        private EmbedBuilder GetApplicationEmbed(Application application)
        {
            var armoryProfileUrl = application.GetFirstAnswerByQuestionPart("wow-armory profile");
            var charAndRealm = ParseArmoryLink(armoryProfileUrl);
            var character = _battleNetApi.GetCharacterData(charAndRealm.realm, charAndRealm.name, "eu").GetAwaiter().GetResult();
            var avatar = _battleNetApi.GetCharacterThumbnail(character, "eu");
            var artifactLevel = _battleNetApi.GetCharacterHeartOfAzerothLevel(character);

            var characterName = application.GetFirstAnswerByQuestionPart("main character");
            var characterSpec = application.GetFirstAnswerByQuestionPart("role/spec");
            var characterClass = application.GetFirstAnswerByQuestionPart("what class");
            var playerName = application.GetFirstAnswerByEitherQuestionPart("your name", "full name");
            var playerAge = application.GetFirstAnswerByQuestionPart("how old are you");
            var playerCountry = application.GetFirstAnswerByQuestionPart("where are you from");

            var embed = new EmbedBuilder();
            embed.Color = new Color(155, 89, 182);
            embed.WithAuthor($"New application @ neon.gg/applications", null, "https://www.neon.gg/applications/");
            embed.WithThumbnailUrl(avatar);

            var title = $"{characterName} - {characterSpec} {characterClass}";
            embed.Title = title.Length > 256 ? title.Substring(0, 256) : title;
            embed.AddField("__Real Name__", playerName, true);
            embed.AddField("__Age__", playerAge, true);
            embed.AddField("__Country__", playerCountry, true);
            embed.AddField("__Character Stats__", $"**Heart of Azeroth level:** {artifactLevel} \r\n**Avg ilvl:** {character.Items.AverageItemLevelEquipped}\r\n**Achi points:** {character.AchievementPoints} | **Total HKs:** {character.TotalHonorableKills}", false);
            embed.AddField("__External sites__", $@"[Armory]({armoryProfileUrl}) | [RaiderIO](https://raider.io/characters/eu/{charAndRealm.realm}/{charAndRealm.name}) | [WoWProgress](https://www.wowprogress.com/character/eu/{charAndRealm.realm}/{charAndRealm.name}) | [WarcraftLogs](https://www.warcraftlogs.com/character/eu/{charAndRealm.realm}/{charAndRealm.name})", false);

            embed.Footer = new EmbedFooterBuilder();
            embed.Footer.WithIconUrl("https://render-eu.worldofwarcraft.com/character/karazhan/102/54145126-avatar.jpg");
            embed.Footer.Text = $"Requested by Veinlash - Today at {DateTime.UtcNow.ToCentralEuropeanTime().ToString("HH:mm")}";

            return embed;
        }

        //handles:
        //https://raider.io/characters/eu/the-maelstrom/Connon
        //https://worldofwarcraft.com/en-gb/character/the-maelstrom/Boop
        //https://www.warcraftlogs.com/character/eu/karazhan/veinlash
        private dynamic ParseArmoryLink(string armoryLink)
        {
            var realmAndNameSplit = armoryLink.Split('?').First().Trim('/').Split('/').Reverse();
            var name = realmAndNameSplit.Skip(0).Take(1).First();
            var realm = realmAndNameSplit.Skip(1).Take(1).First();

            return new {
                realm = realm,
                name = name
            };
        }
    }
}