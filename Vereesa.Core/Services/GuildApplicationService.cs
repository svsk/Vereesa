using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Vereesa.Core.Configuration;
using Vereesa.Core.Extensions;
using Vereesa.Data.Models.EventHub;

namespace Vereesa.Core.Services
{
    public class GuildApplicationService
    {
        private EventHubService _eventHubService;
        private DiscordSocketClient _discord;
        private BattleNetApiService _battleNetApi;
        private GuildApplicationSettings _settings;

        public GuildApplicationService(EventHubService eventHubService, DiscordSocketClient discord, GuildApplicationSettings settings, BattleNetApiService battleNetApi)
        {
            _eventHubService = eventHubService;
            _discord = discord;
            _battleNetApi = battleNetApi;
            _settings = settings;

            Start();
        }

        private void Start()
        {
            _eventHubService.On(EventHubEvents.NewCsvRow).Do(AnnounceNewApplication);
        }

        private void AnnounceNewApplication(object[] applicationData)
        {
            SendApplicationEmbed(applicationData).GetAwaiter().GetResult();
        }

        private async Task SendApplicationEmbed (object[] parameters)
        {
            var notificationChannel = _discord.Guilds.FirstOrDefault()?.Channels.FirstOrDefault(c => c.Name == _settings.NotificationMessageChannelName) as ISocketMessageChannel;
            if (notificationChannel != null) 
            {
                //parse CSV line (gross, I know)
                var row = parameters.FirstOrDefault() as string;
                var application = row.Replace(", ", "¤COMMA¤").Split(',').Select(slug => slug.Replace("¤COMMA¤", ", ")).ToArray();

                try 
                {
                    //await notificationChannel.SendMessageAsync(string.Format(_settings.MessageToSendOnNewLine, fields));
                    await notificationChannel.SendMessageAsync("", false, GetApplicationEmbed(application).Build());
                }
                catch (Exception ex)
                { 
                    //probably a malformed response array
                }
            }
        }

        private EmbedBuilder GetApplicationEmbed(string[] application)
        {
            var armoryProfileUrl = application[5];
            var realmAndName = armoryProfileUrl.Split(new string[] { "/character/" }, StringSplitOptions.None).Last();
            var realmAndNameSplit = realmAndName.Split('/');
            var realm = realmAndNameSplit.Skip(0).Take(1).First();
            var name = realmAndNameSplit.Skip(1).Take(1).First();
            var character = _battleNetApi.GetCharacterData(realm, name, "eu").GetAwaiter().GetResult();
            var avatar = _battleNetApi.GetCharacterThumbnail(character, "eu");
            var artifactTraitCount = _battleNetApi.GetCharacterArtifactTraitCount(character);

            var embed = new EmbedBuilder();
            embed.Color = new Color(155, 89, 182);
            embed.WithAuthor($"New application @ neon.gg/applications", null, "https://www.neon.gg/applications/");
            embed.WithThumbnailUrl(avatar);

            embed.Title = $"{application[9]} - {application[6]} {application[17]}";
            embed.AddField("__Real Name__", application[2], true);
            embed.AddField("__Age__", application[4], true);
            embed.AddField("__Country__", application[3], true);
            embed.AddField("__Character Stats__", $"**Artifact traits:** {artifactTraitCount} \r\n**Health:** {character.Stats.Health.ToString("N")}\r\n**Crit:** {character.Stats.Crit.ToString ("0.##")}% | **Haste:** {character.Stats.Haste.ToString ("0.##")}% | **Mastery:** {character.Stats.Mastery.ToString ("0.##")}%", false);
            embed.AddField("__External sites__", $@"[Armory]({armoryProfileUrl}) | [RaiderIO](https://raider.io/characters/eu/{realm}/{name}) | [WoWProgress](https://www.wowprogress.com/character/eu/{realm}/{name}) | [WarcraftLogs](https://www.warcraftlogs.com/character/eu/{realm}/{name})", false);
            
            embed.Footer = new EmbedFooterBuilder();
            embed.Footer.WithIconUrl("https://render-eu.worldofwarcraft.com/character/karazhan/102/54145126-avatar.jpg");
            embed.Footer.Text = $"Requested by Veinlash - Today at {DateTime.UtcNow.ToCentralEuropeanTime().ToString("HH:mm")}";

            return embed;
        }
    }
}