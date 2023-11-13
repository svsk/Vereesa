using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using NodaTime;
using Vereesa.Core.Configuration;
using Vereesa.Core.Extensions;
using Vereesa.Core.Infrastructure;
using Vereesa.Core.Integrations;

namespace Vereesa.Core.Services
{
    internal class EventArgs
    {
        public int MaxAttendees { get; set; }
        public double RemainingDuration { get; set; }
    }

    public class EventHostService : BotServiceBase
    {
        private static IEmote _joinEmote = new Emoji("✅");
        private static IEmote _declineEmote = new Emoji("❌");
        private readonly IJobScheduler _jobScheduler;
        private readonly OpenAISettings _openAISettings;
        private readonly HttpClient _httpClient;
        private int _promptTimeout = 60000;

        public EventHostService(
            DiscordSocketClient discord,
            IJobScheduler jobScheduler,
            OpenAISettings openAISettings,
            HttpClient httpClient
        )
            : base(discord)
        {
            _jobScheduler = jobScheduler;
            _openAISettings = openAISettings;
            _httpClient = httpClient;
        }

        [OnReaction]
        [Authorize("Guild Leader")]
        public async Task HandleEventRewireReaction(ulong messageId, IMessageChannel channel, SocketReaction reaction)
        {
            if (reaction.Emote.Name != "🗓️")
            {
                return;
            }

            var message = await channel.GetMessageAsync(messageId) as IUserMessage;

            if (TryValidateEventMessage(message, out var eventArgs))
            {
                await WatchForReactions(message, eventArgs.MaxAttendees, eventArgs.RemainingDuration);
                _ = message.RemoveReactionAsync(reaction.Emote, reaction.User.Value);
            }
        }

        private bool TryValidateEventMessage(IUserMessage message, out EventArgs eventArgs)
        {
            eventArgs = null;

            bool IsEventMessage(IUserMessage msg)
            {
                var embed = msg.Embeds.FirstOrDefault();

                if (embed == null)
                    return false;
                if (embed.Author?.Name.Contains("is hosting an event") != true)
                    return false;
                if (!msg.Author.IsBot)
                    return false;
                return true;
            }

            if (message == null)
                return false;
            if (!IsEventMessage(message))
                return false;

            try
            {
                var remainingTimespan = message.Embeds.First().Fields.First(f => f.Name.Contains("Time")).Value;
                var maxAttendees = message.Embeds
                    .First()
                    .Fields.First(f => f.Name.Contains("Signups"))
                    .Value.Split("/")
                    .Last()
                    .Trim();

                var remainingTime = TimeSpan.Parse(remainingTimespan);
                var eventTime = (message.EditedTimestamp ?? message.CreatedAt).Add(remainingTime);

                eventArgs = new EventArgs
                {
                    MaxAttendees = int.Parse(maxAttendees),
                    RemainingDuration = (eventTime - DateTimeOffset.UtcNow).TotalMinutes
                };

                return true;
            }
            catch
            {
                return false;
            }
        }

        private MessageComponent BuildJoinComponents()
        {
            var joinRow = new ActionRowBuilder()
                .WithButton("Tank", "join-tank", ButtonStyle.Primary, new Emoji("🛡️"))
                .WithButton("Ranged DPS", "join-ranged", ButtonStyle.Primary, new Emoji("🏹"))
                .WithButton("Melee DPS", "join-melee", ButtonStyle.Primary, new Emoji("⚔️"))
                .WithButton("Healer", "join-healer", ButtonStyle.Primary, new Emoji("💚"));

            var declineRow = new ActionRowBuilder()
                .WithButton("Decline", "decline-decline", ButtonStyle.Secondary, new Emoji("❌"))
                .WithButton("Tentative", "decline-tentative", ButtonStyle.Secondary, new Emoji("🤷"))
                .WithButton("Late", "decline-late", ButtonStyle.Secondary, new Emoji("🕒"));

            var components = new ComponentBuilder().AddRow(joinRow).AddRow(declineRow);

            return components.Build();
        }

        [OnButtonClick("join-tank")]
        [OnButtonClick("join-ranged")]
        [OnButtonClick("join-melee")]
        [OnButtonClick("join-healer")]
        [AsyncHandler]
        public async Task HandleJoinClicked(SocketMessageComponent interaction)
        {
            var attendees = GetAttendees(interaction.Message);
            attendees.Add(interaction.User.Id.MentionPerson());
            await UpdateAttendeeListAsync(interaction.Message, attendees);
        }

        [OnButtonClick("decline-decline")]
        [AsyncHandler]
        public async Task HandleDeclineClicked(SocketMessageComponent interaction)
        {
            var attendees = GetAttendees(interaction.Message);
            attendees = attendees.Where(d => d != interaction.User.Id.MentionPerson()).ToHashSet();
            await UpdateAttendeeListAsync(interaction.Message, attendees);
        }

        [OnCommand("!scheduleraids")]
        [AsyncHandler]
        [Authorize("Officer")]
        public async Task ScheduleRaids(IMessage message, string numberOfWeeks, string weekDays, string raidStart)
        {
            numberOfWeeks ??= (
                await Prompt(message.Author, "How many weeks do you want to schedule for?", message.Channel, 30000)
            ).Content;

            var numberOfWeeksNumeric = int.Parse(numberOfWeeks);

            weekDays ??= (
                await Prompt(
                    message.Author,
                    "Which days of the week do you want to schedule for?",
                    message.Channel,
                    30000
                )
            ).Content;

            raidStart ??= (
                await Prompt(message.Author, "At what time do the raids start (server time)?", message.Channel, 30000)
            ).Content;

            var ai = new OpenAIClientBuilder(_openAISettings.ApiKey)
                .WithInstruction("Your responses should be pure deserializable JSON at all times.")
                .WithInstruction("Never wrap your responses in JSON objects, they should only be arrays.")
                .Build();

            var daysArray = await ai.QueryAs<string[]>(
                "Give me the weekdays mentioned in this text as a JSON array with full day names in lower case: "
                    + weekDays
            );

            var eventDates = await ai.QueryAs<string[]>(
                $"Given that the current time is {DateTime.UtcNow:u}, consider a time window that ends exactly "
                    + $"{numberOfWeeks} week(s) from this time. "
                    + $"Give me the ISO 8601 dates of every {daysArray.Join(" and ")} within the time window. "
                    + $"The time of each date should be {raidStart} Europe/Paris time. "
                    + $"You should find {numberOfWeeksNumeric * daysArray.Length} dates as a result. "
                    + $"Give me the result a JSON array of one string per date found."
            );

            var guild = (message.Channel as SocketGuildChannel).Guild;
            var voiceChannel = guild.VoiceChannels.FirstOrDefault(
                vc => vc.Name.Contains("Raid", StringComparison.OrdinalIgnoreCase)
            );

            var existingImageUrl = await GetImageUrlFromExistingEvent(guild);
            var eventImage = await CreateImageFromUrl(existingImageUrl);

            foreach (var dateString in eventDates)
            {
                var date = DateTimeOffset.Parse(dateString);

                var discordEvent = await guild.CreateEventAsync(
                    "Neon Raid Night",
                    date,
                    GuildScheduledEventType.Voice,
                    GuildScheduledEventPrivacyLevel.Private,
                    "",
                    date.AddHours(3),
                    voiceChannel.Id,
                    null,
                    eventImage
                );

                // This code blow is pointless if we can't sign people up for events...

                // var hostMessage = BuildHostMessage(
                //     Discord.GetRolesByName("Raider").FirstOrDefault(),
                //     "Neon Raid Night",
                //     message,
                //     30,
                //     3 * 60,
                //     new List<ulong>(),
                //     new RoleDistribution(),
                //     discordEvent.Id,
                //     date,
                //     existingImageUrl,
                //     new Color(0x7565BF)
                // );

                // var joinComponents = BuildJoinComponents();

                // await message.Channel.SendMessageAsync("\n", embed: hostMessage, components: joinComponents);
            }
        }

        private async Task<Image?> CreateImageFromUrl(string imageUrl)
        {
            if (imageUrl != null)
            {
                var stream = await _httpClient.GetStreamAsync(imageUrl);
                return new Image(stream);
            }

            return null;
        }

        private static async Task<string> GetImageUrlFromExistingEvent(SocketGuild guild)
        {
            var existing = (
                await guild
                    .GetEventsAsync()
                    .ToAsyncEnumerable()
                    .FirstOrDefaultAsync(page => page.Any(e => e.Name.Contains("Neon Raid Night")))
            ).FirstOrDefault();

            return existing?.GetCoverImageUrl();
        }

        [OnCommand("!host")]
        [AsyncHandler]
        [Description("Please only answer with a number when prompted for max attendees.")]
        public async Task CreateEventAsync(IMessage triggerMessage)
        {
            var eventName = (
                await Prompt(
                    triggerMessage.Author,
                    "What's the name of the event you're hosting?",
                    triggerMessage.Channel,
                    _promptTimeout
                )
            ).Content;

            var maxAttendees = int.Parse(
                (
                    await Prompt(
                        triggerMessage.Author,
                        "How many people can attend?",
                        triggerMessage.Channel,
                        _promptTimeout
                    )
                ).Content
            );

            var preSignedPeople = await Prompt(
                triggerMessage.Author,
                "Mention anyone who you want to sign up preemptively. Type `none` to skip.",
                triggerMessage.Channel,
                _promptTimeout
            );
            var defaultAttendees = preSignedPeople.MentionedUserIds;

            if (defaultAttendees.Count > maxAttendees)
            {
                await triggerMessage.Channel.SendMessageAsync(
                    "You can't pre-sign more people than the max attendee count."
                );
                return;
            }

            double? eventDurationMinutes = null;
            do
            {
                var eventTime = (
                    await Prompt(
                        triggerMessage.Author,
                        "When will the event begin?\r\n"
                            + "(Simply type a number of minutes from now like `10`"
                            + " **OR** "
                            + "if the event is today you can type a (CET/CEST) time like `20:00`)",
                        triggerMessage.Channel,
                        _promptTimeout
                    )
                ).Content;

                if (!TryParseEventTime(eventTime, out eventDurationMinutes))
                {
                    await triggerMessage.Channel.SendMessageAsync(
                        "I couldn't quite understand what time you meant. "
                            + "Please tell me just a number of minutes like `10` or if the event is today a CET/CEST timestamp "
                            + "like `14:00` or `06:00`."
                    );
                }
            } while (eventDurationMinutes == null);

            var alertRole = await Prompt(
                triggerMessage.Author,
                "Name the role you want me to alert about the event (do not mention it with @, just give me the name)."
                    + "Type `none` to skip.",
                triggerMessage.Channel,
                _promptTimeout
            );
            var role = Discord.GetRolesByName(alertRole.Content).FirstOrDefault();

            var hostMessageEmbed = BuildHostMessage(
                role,
                eventName,
                triggerMessage,
                maxAttendees,
                eventDurationMinutes.Value,
                defaultAttendees
            );

            var hostMessage = await triggerMessage.Channel.SendMessageAsync(embed: hostMessageEmbed);
            if (defaultAttendees.Count >= maxAttendees)
            {
                await UpdateStatusAsync(hostMessage, "🔴", "Closed");
            }
            else
            {
                _ = WatchForReactions(hostMessage, maxAttendees, eventDurationMinutes.Value);
            }
        }

        private bool TryParseEventTime(string eventTime, out double? remainingMinutes)
        {
            remainingMinutes = null;

            try
            {
                if (eventTime.Contains(":"))
                {
                    var (hours, minutes, rest) = eventTime.Split(":").Select(int.Parse).ToList();
                    var now = DateTimeOffset.Now.ToCentralEuropeanTime();
                    var startOfDay = now.Date;
                    var eventStart = startOfDay.AddHours(hours).AddMinutes(minutes);

                    remainingMinutes = (eventStart - now).TotalMinutes;
                }
                else
                {
                    remainingMinutes = double.Parse(eventTime);
                }
            }
            catch
            {
                return false;
            }

            return remainingMinutes != null;
        }

        private static Embed BuildHostMessage(
            SocketRole role,
            string eventName,
            IMessage triggerMessage,
            int maxAttendees,
            double eventDurationMinutes,
            IReadOnlyCollection<ulong> defaultAttendees,
            RoleDistribution roleDistribution = null,
            ulong? eventId = null,
            DateTimeOffset? startDate = null,
            string imageUrl = null,
            Color? color = null
        )
        {
            var roleMention = role?.Mention != null ? $" {role.Mention}" : "";

            var attendees = defaultAttendees.Any()
                ? defaultAttendees.Select(uid => uid.MentionPerson()).Join(" ")
                : "No attendees yet";

            var time = Duration.FromMinutes(eventDurationMinutes).ToString("HH:mm:ss", CultureInfo.InvariantCulture);

            var author =
                startDate != null
                    ? "📆 "
                        + startDate.Value
                            .ToCentralEuropeanTime()
                            .ToString("ddd MMM d ∙ HH:mm", CultureInfo.InvariantCulture)
                        + " CET"
                    : $"{triggerMessage.Author.GetPreferredDisplayName()} is hosting an event";

            var timeHeader = startDate != null ? "🕒 Duration" : "🕒 Time";

            var builder = new EmbedBuilder()
                .WithColor(color ?? Color.Gold)
                .WithAuthor(author)
                .WithTitle(eventName)
                .WithDescription($"Hey{roleMention}, it's time for **{eventName}**!")
                .AddField("🟢 Status", "Open", true)
                .AddField("🙋‍♂️ Signups", $"{defaultAttendees.Count}/{maxAttendees}", true)
                .AddField(timeHeader, time, true);

            if (eventId != null)
            {
                builder.WithFooter(fb =>
                {
                    fb.WithText($"EID:{eventId}");
                });
            }

            if (imageUrl != null)
            {
                builder.WithImageUrl(imageUrl);
            }

            if (roleDistribution != null)
            {
                builder.AddField(
                    $"🛡️ ({roleDistribution.Tanks.Count})",
                    roleDistribution.Tanks.Any() ? roleDistribution.Tanks.Join(" ") : "None",
                    true
                );
                builder.AddField(
                    $"🏹 ({roleDistribution.RangedDps.Count})",
                    roleDistribution.RangedDps.Any() ? roleDistribution.RangedDps.Join(" ") : "None",
                    true
                );
                builder.AddField(
                    $"⚔️ ({roleDistribution.MeleeDps.Count})",
                    roleDistribution.MeleeDps.Any() ? roleDistribution.MeleeDps.Join(" ") : "None",
                    true
                );
                builder.AddField(
                    $"💚 ({roleDistribution.Healers.Count})",
                    roleDistribution.Healers.Any() ? roleDistribution.Healers.Join(" ") : "None",
                    true
                );
            }

            builder.AddField("Attendees", attendees);

            return builder.Build();
        }

        private async Task WatchForReactions(IUserMessage hostMessage, int maxAttendees, double eventDurationMinutes)
        {
            var expirationInstant = SystemClock.Instance
                .GetCurrentInstant()
                .Plus(Duration.FromMinutes(eventDurationMinutes));

            Discord.ReactionAdded += ReactionHandler;
            Discord.ReactionRemoved += ReactionHandler;

            _jobScheduler.EveryHalfMinute += ProgressTowardEventExpiration;

            _jobScheduler.Schedule(
                expirationInstant,
                async () =>
                {
                    await EndWatchAsync();
                }
            );

            foreach (var relevantReaction in await GetRelevantReactions(hostMessage))
            {
                await HandleReaction(
                    hostMessage,
                    hostMessage.Channel,
                    relevantReaction.user,
                    new Emoji(relevantReaction.emoteName)
                );
            }

            await hostMessage.AddReactionsAsync(new[] { _joinEmote, _declineEmote });

            async Task ReactionHandler(
                Cacheable<IUserMessage, ulong> message,
                Cacheable<IMessageChannel, ulong> channel,
                SocketReaction reaction
            ) =>
                await HandleReaction(
                    await message.GetOrDownloadAsync(),
                    await channel.GetOrDownloadAsync(),
                    reaction.User.Value,
                    reaction.Emote
                );

            async Task HandleReaction(IUserMessage message, IMessageChannel channel, IUser user, IEmote reactionEmote)
            {
                if (user.IsBot)
                {
                    return;
                }

                if (message.Id == hostMessage.Id)
                {
                    _ = message.RemoveReactionAsync(reactionEmote, user.Id);
                    var attendees = GetAttendees(hostMessage);

                    if (reactionEmote.Name == _joinEmote.Name)
                    {
                        attendees.Add(user.Id.MentionPerson());
                    }

                    if (reactionEmote.Name == _declineEmote.Name)
                    {
                        attendees.Remove(user.Id.MentionPerson());
                    }

                    await UpdateAttendeeListAsync(hostMessage, attendees);

                    if (attendees.Count >= maxAttendees)
                    {
                        await EndWatchAsync();
                    }
                }
            }

            async Task ProgressTowardEventExpiration()
            {
                var attendees = GetAttendees(hostMessage);
                await UpdateAttendeeListAsync(hostMessage, attendees);
                await UpdateExpirationTimerAsync(hostMessage, expirationInstant);
            }

            async Task EndWatchAsync()
            {
                _jobScheduler.EveryHalfMinute -= ProgressTowardEventExpiration;
                Discord.ReactionAdded -= ReactionHandler;
                Discord.ReactionRemoved -= ReactionHandler;
                await UpdateStatusAsync(hostMessage, "🔴", "Closed");
            }
        }

        private async Task<List<(IUser user, string emoteName)>> GetRelevantReactions(IUserMessage hostMessage)
        {
            var result = new List<(IUser user, string emoteName)>();

            foreach (var reaction in hostMessage.Reactions)
            {
                if (reaction.Key.Name == _joinEmote.Name)
                {
                    var accepts = await hostMessage.GetReactionUsersAsync(reaction.Key, 50).ToListAsync();
                    result.AddRange(accepts.SelectMany(e => e).Select(e => (e, _joinEmote.Name)));
                }

                if (reaction.Key.Name == _declineEmote.Name)
                {
                    var declines = await hostMessage.GetReactionUsersAsync(reaction.Key, 50).ToListAsync();
                    result.AddRange(declines.SelectMany(e => e).Select(e => (e, _declineEmote.Name)));
                }
            }

            return result;
        }

        private HashSet<string> GetAttendees(IUserMessage hostMessage)
        {
            return hostMessage.Embeds
                .First()
                .Fields.FirstOrDefault(f => f.Name.Contains("Attendees"))
                .Value.ToString()
                .Split(" ")
                .Where(s => s.StartsWith("<@"))
                .ToHashSet();
        }

        private async Task UpdateExpirationTimerAsync(IUserMessage hostMessage, Instant expirationInstant)
        {
            var updatedEmbedBuilder = hostMessage.Embeds.First().ToEmbedBuilder();
            var timeField = updatedEmbedBuilder.Fields.FirstOrDefault(f => f.Name.Contains("Time"));

            var remaining = expirationInstant - SystemClock.Instance.GetCurrentInstant();
            timeField.Value = remaining.ToString("HH:mm:ss", CultureInfo.InvariantCulture);

            await hostMessage.ModifyAsync(msg => msg.Embed = updatedEmbedBuilder.Build());
        }

        private async Task UpdateAttendeeListAsync(IUserMessage hostMessage, HashSet<string> updatedAttendees)
        {
            var existingEmbed = hostMessage.Embeds.FirstOrDefault();
            if (existingEmbed == null)
            {
                return;
            }

            var updatedEmbedBuilder = existingEmbed.ToEmbedBuilder();
            var attendeeCountField = updatedEmbedBuilder.Fields.FirstOrDefault(f => f.Name.Contains("Signups"));
            var attendeesListField = updatedEmbedBuilder.Fields.FirstOrDefault(f => f.Name.Contains("Attendees"));

            var maxAttendees = attendeeCountField.Value.ToString().Split("/").Last().Trim();
            var attendees = updatedAttendees.Any() ? updatedAttendees.Join(" ") : "No attendees yet";
            attendeeCountField.Value = $"{updatedAttendees.Count}/{maxAttendees}";
            attendeesListField.Value = attendees;

            await hostMessage.ModifyAsync(msg => msg.Embed = updatedEmbedBuilder.Build());
        }

        private async Task UpdateStatusAsync(IUserMessage hostMessage, string newEmoji, string statusText)
        {
            var updatedEmbedBuilder = hostMessage.Embeds.First().ToEmbedBuilder();
            var statusField = updatedEmbedBuilder.Fields.FirstOrDefault(f => f.Name.Contains("Status"));

            statusField.Name = $"{newEmoji} Status";
            statusField.Value = statusText;

            await hostMessage.ModifyAsync(msg => msg.Embed = updatedEmbedBuilder.Build());
        }
    }

    public class RoleDistribution
    {
        public HashSet<string> Tanks { get; set; } = new();
        public HashSet<string> RangedDps { get; set; } = new();
        public HashSet<string> MeleeDps { get; set; } = new();
        public HashSet<string> Healers { get; set; } = new();
    }
}
