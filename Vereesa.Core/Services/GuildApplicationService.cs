using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Vereesa.Core.Configuration;
using Vereesa.Data.Models.EventHub;

namespace Vereesa.Core.Services
{
    public class GuildApplicationService
    {
        private EventHubService _eventHubService;
        private DiscordSocketClient _discord;
        private GuildApplicationSettings _settings;

        public GuildApplicationService(EventHubService eventHubService, DiscordSocketClient discord, GuildApplicationSettings settings)
        {
            _eventHubService = eventHubService;
            _discord = discord;
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
                var fields = row.Replace(", ", "¤COMMA¤").Split(',').Select(slug => slug.Replace("¤COMMA¤", ", ")).ToArray();

                try 
                {
                    //await notificationChannel.SendMessageAsync(string.Format(_settings.MessageToSendOnNewLine, fields));
                    await notificationChannel.SendMessageAsync("", false, GetApplicationEmbed("test"));
                }
                catch (Exception ex)
                { 
                    //probably a malformed response array
                }
            }
        }

        private EmbedBuilder GetApplicationEmbed(string application)
        {
            var embed = new EmbedBuilder();
            embed.Title = "Test";
            embed.Color = new Color(155, 89, 182);
            
            embed.WithThumbnailUrl("https://render-eu.worldofwarcraft.com/character/karazhan/102/54145126-avatar.jpg");
            embed.WithAuthor("Veinlash - Karazhan EU", "https://render-eu.worldofwarcraft.com/character/karazhan/102/54145126-avatar.jpg", "https://worldofwarcraft.com/en-gb/character/karazhan/veinlash");

            embed.AddField("__Name__", "Sverre Skuland", true);
            embed.AddField("__Class__", "Death Knight", true);
            embed.AddField("__Specialization__", "Blood", true);
            embed.AddField("__Character Stats__", "**Artifact traits:** 78 \r\n**Health:** 5 493 060\r\n**Crit:** 31.56% | **Haste:** 22.09% | **Mastery:** 51.04%", false);
            embed.AddField("__External sites__", @"[Armory](https://www.google.com) | [RaiderIO](https://www.google.com) | [WoWProgress](https://www.google.com) | [WarcraftLogs](https://www.google.co)", false);
            
            embed.Footer = new EmbedFooterBuilder();
            embed.Footer.WithIconUrl("https://render-eu.worldofwarcraft.com/character/karazhan/102/54145126-avatar.jpg");
            embed.Footer.Text = "Requested by Veinlash - Today at 9:50 AM";

            return embed;
        }
    }
}