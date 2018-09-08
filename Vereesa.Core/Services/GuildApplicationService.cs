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
            TimerHelpers.SetTimeout(() => {
                var applications = _neonApiService.GetApplications();

                if (applications != null) 
                {
                    CheckForNewApplications(applications).GetAwaiter().GetResult();
                }
            }, 30000, true, true);
        }

        private async Task CheckForNewApplications(IEnumerable<Application> applications)
        {
            var isFirstRun = _cachedApplications == null;

            if (isFirstRun)
            {
                _cachedApplications = new Dictionary<int, Application>();
            }

            foreach (var application in applications) 
            {
                if (!_cachedApplications.ContainsKey(application.Id) && !isFirstRun)
                {
                    await AnnounceNewApplication(application);
                }

                _cachedApplications[application.Id] = application;
            }

            
        }

        private async Task AnnounceNewApplication (Application application)
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
                    //probably a malformed response array
                }
            }
        }

        private EmbedBuilder GetApplicationEmbed(Application application)
        {
            var armoryProfileUrl = application.GetFirstAnswerByQuestionPart("wow-armory profile");
            var realmAndName = armoryProfileUrl.Split(new string[] { "/character/" }, StringSplitOptions.None).Last();
            var realmAndNameSplit = realmAndName.Split('/');
            var realm = realmAndNameSplit.Skip(0).Take(1).First();
            var name = realmAndNameSplit.Skip(1).Take(1).First();
            var character = _battleNetApi.GetCharacterData(realm, name, "eu").GetAwaiter().GetResult();
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

            embed.Title = $"{characterName} - {characterSpec} {characterClass}";
            embed.AddField("__Real Name__", playerName, true);
            embed.AddField("__Age__", playerAge, true);
            embed.AddField("__Country__", playerCountry, true);
            embed.AddField("__Character Stats__", $"**Heart of Azeroth level:** {artifactLevel} \r\n**Avg ilvl:** {character.Items.AverageItemLevelEquipped}\r\n**Achi points:** {character.AchievementPoints} | **Total HKs:** {character.TotalHonorableKills}", false);
            embed.AddField("__External sites__", $@"[Armory]({armoryProfileUrl}) | [RaiderIO](https://raider.io/characters/eu/{realm}/{name}) | [WoWProgress](https://www.wowprogress.com/character/eu/{realm}/{name}) | [WarcraftLogs](https://www.warcraftlogs.com/character/eu/{realm}/{name})", false);
            
            embed.Footer = new EmbedFooterBuilder();
            embed.Footer.WithIconUrl("https://render-eu.worldofwarcraft.com/character/karazhan/102/54145126-avatar.jpg");
            embed.Footer.Text = $"Requested by Veinlash - Today at {DateTime.UtcNow.ToCentralEuropeanTime().ToString("HH:mm")}";

            return embed;
        }
    }
}