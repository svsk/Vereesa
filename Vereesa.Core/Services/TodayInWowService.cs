using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Vereesa.Core.Extensions;
using Vereesa.Core.Helpers;
using Vereesa.Core.Integrations;
using Vereesa.Core.Integrations.Interfaces;
using Vereesa.Data.Models.Wowhead;

namespace Vereesa.Core.Services 
{
    public class TodayInWoWService 
    {
        private bool _isInitialized = false;
        private DiscordSocketClient _discord;
        private IWowheadClient _wowhead;

        public TodayInWoWService(DiscordSocketClient discord, IWowheadClient wowhead) 
        {
            _discord = discord;
            _wowhead = wowhead;
            _discord.Ready += InitializeServiceAsync;
        }

        private async Task InitializeServiceAsync()
        {
            if (_isInitialized)
                return;

            _isInitialized = true;

            // TimerHelpers.SetTimeout(() => {
            //     DateTime cetNow = DateTime.UtcNow.ToCentralEuropeanTime();
            //     if (cetNow.Hour == 9 && cetNow.Minute == 0) 
            //     {
            //         AnnounceTodayInWow();
            //     }
            // }, 1000 * 60, true, true);
        }

        private void AnnounceTodayInWow()
        {
            var todayInWow = _wowhead.GetTodayInWow();

            _discord.Guilds.First(g => g.Name == "Neon").GetChannelByName("botplayground").SendMessageAsync(string.Empty, embed: GenerateEmbed(todayInWow));
        }

        private Embed GenerateEmbed(TodayInWow todayInWow)
        {
            var embed = new EmbedBuilder();
            embed.Color = new Color(155, 89, 182);

            var title = $"Today in WoW";
            embed.Title = title.Length > 256 ? title.Substring(0, 256) : title;

            foreach (var section in todayInWow.Sections) 
            {
                embed.AddField($"__{section.Title}__", new List<string>{ "Test", "Hest", "Lest"}, true);
                section.Entries.ForEach(e => embed.AddField($"{e}", new List<string>{ "Test", "Hest", "Lest"}, true));
            }
            
            // embed.AddField("__Age__", playerAge, true);
            // embed.AddField("__Country__", playerCountry, true);
            // embed.AddField("__Status__", GetIconedStatusString(application.CurrentStatusString), true);
            // embed.AddField("__Character Stats__", $"**Heart of Azeroth level:** {artifactLevel} \r\n**Avg ilvl:** {character.Items.AverageItemLevelEquipped}\r\n**Achi points:** {character.AchievementPoints} | **Total HKs:** {character.TotalHonorableKills}", false);
            // embed.AddField("__External sites__", $@"[Armory]({armoryProfileUrl}) | [RaiderIO](https://raider.io/characters/eu/{charAndRealm.realm}/{charAndRealm.name}) | [WoWProgress](https://www.wowprogress.com/character/eu/{charAndRealm.realm}/{charAndRealm.name}) | [WarcraftLogs](https://www.warcraftlogs.com/character/eu/{charAndRealm.realm}/{charAndRealm.name})", false);

            embed.Footer = new EmbedFooterBuilder();
            embed.Footer.WithIconUrl("https://render-eu.worldofwarcraft.com/character/karazhan/102/54145126-avatar.jpg");
            embed.Footer.Text = $"Requested by Veinlash - Today at {DateTime.UtcNow.ToCentralEuropeanTime().ToString("HH:mm")}";

            return embed.Build();
        }
    }
}