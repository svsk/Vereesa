using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
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
        private int _promptTimeout = 60000;

        public EventHostService(DiscordSocketClient discord, IJobScheduler jobScheduler, OpenAISettings openAISettings)
            : base(discord)
        {
            _jobScheduler = jobScheduler;
            _openAISettings = openAISettings;
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
            var rowBuilder = new ActionRowBuilder()
                .WithButton("Accept", "accept-btn", ButtonStyle.Success)
                .WithButton("Decline", "decline-btn", ButtonStyle.Danger)
                .WithButton("Tentative", "tentative-btn", ButtonStyle.Secondary);

            var components = new ComponentBuilder().AddRow(rowBuilder);

            return components.Build();
        }

        [OnCommand("!scheduleraids")]
        [AsyncHandler]
        public async Task ScheduleRaids(IMessage message)
        {
            var numberOfWeeks = await Prompt(
                message.Author,
                "How many weeks do you want to schedule for?",
                message.Channel,
                30000
            );

            var numberOfWeeksNumeric = int.Parse(numberOfWeeks.Content);

            var days = await Prompt(
                message.Author,
                "Which days of the week do you want to schedule for?",
                message.Channel,
                30000
            );

            var raidStart = await Prompt(
                message.Author,
                "At what time do the raids start (server time)?",
                message.Channel,
                30000
            );

            var ai = new OpenAIClientBuilder(_openAISettings.ApiKey)
                .WithInstruction("Your responses should be pure deserializable JSON at all times.")
                .WithInstruction("Never wrap your responses in JSON objects, they should only be arrays.")
                .Build();

            var daysArray = await ai.QueryAs<string[]>(
                "Give me the weekdays mentioned in this text as a JSON array with full day names in lower case: "
                    + days.Content
            );

            var eventDates = await ai.QueryAs<string[]>(
                $"Given that the current time is {DateTime.UtcNow:u}, consider a time window that ends exactly "
                    + $"{numberOfWeeks.Content} week(s) from this time. "
                    + $"Give me the ISO 8601 dates of every {daysArray.Join(" and ")} within the time window. "
                    + $"The time of each date should be {raidStart.Content} Europe/Paris time. "
                    + $"You should find {numberOfWeeksNumeric * daysArray.Length} dates as a result. "
                    + $"Give me the result a JSON array of one string per date found."
            );

            // await message.Channel.SendMessageAsync(
            //     "Here are the dates I found:\n"
            //         + eventDates.Select(d => DateTimeOffset.Parse(d).ToString("u")).Join("\n")
            // );

            foreach (var dateString in eventDates)
            {
                var date = DateTimeOffset.Parse(dateString);

                var hostMessage = BuildHostMessage(
                    Discord.GetRolesByName("Raider").FirstOrDefault(),
                    "Neon Raid Night " + date.ToCentralEuropeanTime(),
                    message,
                    30,
                    3 * 60,
                    new List<ulong>()
                );

                var joinComponents = BuildJoinComponents();

                await message.Channel.SendMessageAsync("\n", embed: hostMessage, components: joinComponents);
            }
        }

        [OnSelectMenuExecuted("role-select")]
        [AsyncHandler]
        public async Task HandleRoleSelected(SocketMessageComponent interaction)
        {
            if (interaction.Data is { Values: string[] } data)
            {
                // await interaction.Message.ModifyAsync(msg =>
                // {
                //     msg.Content = "You chose to join as: " + data.Values.FirstOrDefault();
                //     msg.Components = null;
                // });
            }
        }

        [OnButtonClick("accept-btn")]
        [AsyncHandler]
        public async Task HandleJoinClicked(SocketMessageComponent interaction)
        {
            var selectMenuBuilder = new SelectMenuBuilder()
                .WithCustomId("role-select")
                .WithOptions(
                    new()
                    {
                        new() { Label = "Ranged DPS", Value = "ranged" },
                        new() { Label = "Melee DPS", Value = "melee" },
                        new() { Label = "Healer", Value = "healer" },
                        new() { Label = "Tank", Value = "tank" }
                    }
                );

            var rowBuilder1 = new ActionRowBuilder().WithSelectMenu(selectMenuBuilder);

            var components = new ComponentBuilder().AddRow(rowBuilder1);

            await interaction.FollowupAsync(
                "Please select your role.",
                ephemeral: true,
                components: components.Build()
            );
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

        private Embed BuildHostMessage(
            SocketRole role,
            string eventName,
            IMessage triggerMessage,
            int maxAttendees,
            double eventDurationMinutes,
            IReadOnlyCollection<ulong> defaultAttendees
        )
        {
            var roleMention = role?.Mention != null ? $" {role.Mention}" : "";

            var attendees = defaultAttendees.Any()
                ? defaultAttendees.Select(uid => uid.MentionPerson()).Join(" ")
                : "No attendees yet";

            var time = Duration.FromMinutes(eventDurationMinutes).ToString("HH:mm:ss", CultureInfo.InvariantCulture);

            var builder = new EmbedBuilder()
                .WithColor(Color.Gold)
                .WithAuthor($"{triggerMessage.Author.GetPreferredDisplayName()} is hosting an event")
                .WithTitle(eventName)
                .WithDescription($"Hey{roleMention}, it's time for **{eventName}**!")
                .AddField("🟢 Status", "Open", true)
                .AddField("🙋‍♂️ Signups", $"{defaultAttendees.Count}/{maxAttendees}", true)
                .AddField("🕒 Time", time, true)
                .AddField("Attendees", attendees);

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
}
