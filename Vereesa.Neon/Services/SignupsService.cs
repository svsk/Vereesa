using System.Net;
using Discord;
using Discord.Rest;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Vereesa.Neon.Configuration;
using Vereesa.Neon.Exceptions;
using Vereesa.Neon.Extensions;
using Vereesa.Neon.Helpers;
using Vereesa.Core.Infrastructure;
using Timer = System.Timers.Timer;
using Vereesa.Core;

namespace Vereesa.Neon.Services
{
    public class SignupsService : IBotService
    {
        private ILogger<SignupsService> _logger;
        private SignupsSettings _settings;
        private long _lastEventUpdate;
        private Timer _refreshInterval;
        private SignupReport _lastSignupReport;
        private bool _runningRefresh;

        public SignupsService(SignupsSettings settings, ILogger<SignupsService> logger)
        {
            _logger = logger;
            _settings = settings;
        }

        [OnCommand("!signups")]
        public async Task EvaluateMessageAsync(IMessage message)
        {
            await AnnounceSignupsDeprecationAsync(message.Channel);
        }

        private async Task AnnounceSignupsDeprecationAsync(IMessageChannel channel)
        {
            await channel.SendMessageAsync(
                "Since Blizzard's 8.0.0 update to World of Warcraft, changing guilds to communities, I am no longer able to check signups for guild events. Tell Blizzard you want calendar data through official APIs in this forum thread and it may yet return: https://us.battle.net/forums/en/bnet/topic/13979457879"
            );
        }

        private async Task ReportSignupsAsync(ISocketMessageChannel targetChannel, SocketUser requestedBy)
        {
            try
            {
                var nextEvent = (await GetEventsAsync()).First();
                var dataIsOld =
                    DateTime.Now - DateTimeOffset.FromUnixTimeSeconds(_lastEventUpdate) > TimeSpan.FromMinutes(30);
                var reportMessage = await targetChannel.SendMessageAsync(
                    string.Empty,
                    false,
                    BuildEventEmbed(nextEvent, requestedBy, dataIsOld)
                );

                if (dataIsOld && _runningRefresh == false)
                {
                    _lastSignupReport = new SignupReport
                    {
                        Event = nextEvent,
                        DiscordMessage = reportMessage,
                        RequestedBy = requestedBy
                    };

                    _runningRefresh = true;
                    await RefreshSignupsAsync();
                    await InitializeRefreshCheckerAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, ex);
                await targetChannel.SendMessageAsync("I wasn't able to check for the signups to the next raid. Sorry!");
            }
        }

        private async Task UpdateExistingEventEmbedAsync(IEnumerable<CalendarEvent> events)
        {
            if (_lastSignupReport == null)
                return;

            var newEmbed = BuildEventEmbed(events.First(), _lastSignupReport.RequestedBy, false);

            await _lastSignupReport.DiscordMessage.ModifyAsync((msg) => msg.Embed = newEmbed);
        }

        private async Task<IEnumerable<CalendarEvent>> GetEventsAsync()
        {
            using (var client = new HttpClient())
            {
                var httpResult = await client.GetAsync(_settings.EventsUrl);

                if (httpResult.StatusCode == HttpStatusCode.OK)
                {
                    var eventJson = await httpResult.Content.ReadAsStringAsync();
                    var response = JsonConvert.DeserializeObject<Dictionary<string, object>>(eventJson);

                    var lastUpdated = (long)response["lastUpdated"];
                    var eventsJson = response["events"].ToString();

                    _lastEventUpdate = lastUpdated;
                    return JsonConvert.DeserializeObject<List<CalendarEvent>>(eventsJson);
                }
                else
                {
                    throw new UnexpectedResultException(
                        $"{_settings.EventsUrl} returned unexpected HTTP status ({httpResult.StatusCode})."
                    );
                }
            }
        }

        private async Task RefreshSignupsAsync()
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Authorization", _settings.ApiKey);
                var httpResult = await client.GetAsync(_settings.EventsUpdateUrl);
            }
        }

        private async Task InitializeRefreshCheckerAsync()
        {
            _refreshInterval = await TimerHelpers.SetTimeoutAsync(
                async () =>
                {
                    var previousLastRefresh = _lastEventUpdate;
                    var events = await GetEventsAsync();

                    if (previousLastRefresh < _lastEventUpdate)
                    {
                        _refreshInterval.Stop();
                        _runningRefresh = false;
                        await UpdateExistingEventEmbedAsync(events);
                    }
                },
                10000,
                true
            );
        }

        private Embed BuildEventEmbed(CalendarEvent calendarEvent, SocketUser requestedBy, bool dataIsOld)
        {
            var declineStatuses = new string[] { "Declined", "Out" };
            var acceptedStatuses = new string[] { "Accepted", "Confirmed", "Standby" };

            var acceptedCharacters = calendarEvent.InvitationList
                .Where(person => acceptedStatuses.Contains(person.Status))
                .ToList();
            var declinedCharacters = calendarEvent.InvitationList
                .Where(person => declineStatuses.Contains(person.Status))
                .ToList();
            var invitedCharacters = calendarEvent.InvitationList.Where(person => person.Status == "Invited").ToList();

            var acceptedCharList = acceptedCharacters.Any()
                ? string.Join(", ", acceptedCharacters.Select(c => $"{c.Name}"))
                : "None";
            var declinedCharList = declinedCharacters.Any()
                ? string.Join(", ", declinedCharacters.Select(c => $"{c.Name}"))
                : "None";
            var invitedCharList = invitedCharacters.Any()
                ? string.Join(", ", invitedCharacters.Select(c => $"{c.Name}"))
                : "None";

            var embed = new EmbedBuilder();
            embed.Color = new Color(155, 89, 182);

            embed.WithAuthor($"{calendarEvent.Name}");
            embed.WithThumbnailUrl("https://cdn.sverr.es/2018-07-02_16-43-45.png");

            embed.Title = $"{calendarEvent.Type} - {calendarEvent.Instance}";
            embed.AddField("__Description__", calendarEvent.Description, true);
            embed.AddField("__Date__", calendarEvent.Time, true);
            embed.AddField($"__Accepted players ({acceptedCharacters.Count})__", $"{acceptedCharList}", true);
            embed.AddField($"__Declined players ({declinedCharacters.Count})__", $"{declinedCharList}", true);
            embed.AddField($"__Invited players ({invitedCharacters.Count})__", $"{invitedCharList}", false);

            if (dataIsOld)
            {
                embed.AddField(
                    ":warning: Warning",
                    "️This data is more than 30 minutes old. I'm currently running a refresh. This embed will get updated as soon as fresh data is available."
                );
            }

            if (requestedBy != null)
            {
                embed.Footer = new EmbedFooterBuilder();
                embed.Footer.WithIconUrl(requestedBy.GetAvatarUrl());
                embed.Footer.Text =
                    $"Requested by {requestedBy.Username} - Today at {DateTimeExtensions.NowInCentralEuropeanTime().ToString("HH:mm")}";
            }

            return embed.Build();
        }

        //Service classes

        private class SignupReport
        {
            public RestUserMessage DiscordMessage { get; set; }
            public CalendarEvent Event { get; set; }
            public SocketUser RequestedBy { get; set; }
        }

        private class CalendarEvent
        {
            public string Name { get; set; }
            public string Type { get; set; }
            public string Instance { get; set; }
            public string Description { get; set; }
            public string Time { get; set; }
            public IEnumerable<EventAttendee> InvitationList { get; set; }
        }

        private class EventAttendee
        {
            public string Name { get; set; }
            public string Realm { get; set; }
            public string Class { get; set; }
            public string Status { get; set; }
        }
    }
}
