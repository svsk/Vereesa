using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Timers;
using Discord.WebSocket;
using Vereesa.Core.Configuration;

namespace Vereesa.Core.Services
{
    public class GoogleSheetService
    {
        private DiscordSocketClient _discord;
        private GoogleSheetSettings _settings;
        private Timer _checkInterval;
        private int _previousRowCount;

        public GoogleSheetService(DiscordSocketClient discord, GoogleSheetSettings settings)
        {
            _discord = discord;
            _settings = settings;
            _discord.GuildAvailable += OnGuildAvailable;
            _previousRowCount = -1;
        }

        private async Task OnGuildAvailable(SocketGuild guild)
        {
            if (_checkInterval != null)
                _checkInterval.Stop();

            _checkInterval = new Timer();
            _checkInterval.Interval = _settings.CheckIntervalSeconds * 1000;
            _checkInterval.AutoReset = true;
            _checkInterval.Elapsed += (object sender, ElapsedEventArgs e) => { ReadSheet(); };
            _checkInterval.Start();
            ReadSheet();

            //Supress warning... Blerh
            await Task.Run(() => {});
        }

        private async void ReadSheet()
        {
            var rows = new List<string>();
            HttpResponseMessage response = null;

            using (var client = new HttpClient())
            {
                try 
                {
                    response = await client.GetAsync(_settings.GoogleSheetCsvUrl);
                }
                catch (TaskCanceledException) 
                {
                    //aw well
                }
            }
                
            if (response != null && response.StatusCode == HttpStatusCode.OK)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                rows = responseContent.Split(new[] { Environment.NewLine }, StringSplitOptions.None).ToList();

                if (_previousRowCount != -1 && rows.Count > _previousRowCount)
                {
                    var notificationChannel = _discord.Guilds.FirstOrDefault()?.Channels.FirstOrDefault(c => c.Name == _settings.NotificationMessageChannelName) as ISocketMessageChannel;
                    if (notificationChannel != null) 
                    {
                        foreach (var row in rows.Skip(_previousRowCount))
                        {
                            //parse CSV line (gross, I know)
                            var fields = row.Replace(", ", "¤COMMA¤").Split(',').Select(slug => slug.Replace("¤COMMA¤", ", ")).ToArray();
                            try 
                            {
                                await notificationChannel.SendMessageAsync(string.Format(_settings.MessageToSendOnNewLine, fields));
                            }
                            catch (Exception ex)
                            { 
                                //probably a malformed response array
                            }
                            
                        }
                    }
                }

                _previousRowCount = rows.Count;
            }
        }
    }
}