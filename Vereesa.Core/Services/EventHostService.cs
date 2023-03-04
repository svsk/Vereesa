using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using NodaTime;
using Vereesa.Core.Extensions;
using Vereesa.Core.Infrastructure;

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
		private int _promptTimeout = 60000;

		public EventHostService(DiscordSocketClient discord, IJobScheduler jobScheduler) : base(discord)
		{
			_jobScheduler = jobScheduler;
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
				if (!msg.Content.StartsWith("Hey, it's time for")) return false;
				if (!msg.Content.Contains("people may join")) return false;
				if (!msg.Author.IsBot) return false;
				return true;
			}

			if (message == null) return false;
			if (!IsEventMessage(message)) return false;

			try
			{
				var remainingTimespan = message.Content.Substring(message.Content.IndexOf("🕒"))
					.Split(' ')
					.Skip(1)
					.First()
					.Replace("\r", "")
					.Replace("\n🙋‍♂️", "");

				var remainingTime = TimeSpan.Parse(remainingTimespan);
				var eventTime = (message.EditedTimestamp ?? message.CreatedAt).Add(remainingTime);

				eventArgs = new EventArgs
				{
					MaxAttendees = int.Parse(
						message.Content.Substring(0, message.Content.IndexOf(" people may join"))
							.Split(' ')
							.Last()
					),
					RemainingDuration = (eventTime - DateTimeOffset.UtcNow).TotalMinutes
				};

				return true;
			}
			catch
			{
				return false;
			}
		}

		[OnCommand("!host")]
		[AsyncHandler]
		[Description("Please only answer with a number when prompted for max attendees.")]
		public async Task CreateEventAsync(IMessage triggerMessage)
		{
			var eventName = (await Prompt(triggerMessage.Author, "What's the name of the event you're hosting?", triggerMessage.Channel, _promptTimeout)).Content;

			var maxAttendees = int.Parse((await Prompt(triggerMessage.Author, "How many people can attend?", triggerMessage.Channel, _promptTimeout)).Content);

			var preSignedPeople = await Prompt(triggerMessage.Author, "Mention anyone who you want to sign up preemptively. Type `none` to skip.", triggerMessage.Channel, _promptTimeout);
			var defaultAttendees = preSignedPeople.MentionedUserIds;

			if (defaultAttendees.Count > maxAttendees)
			{
				await triggerMessage.Channel.SendMessageAsync("You can't pre-sign more people than the max attendee count.");
				return;
			}

			double? eventDurationMinutes = null;
			do
			{
				var eventTime = (await Prompt(
					triggerMessage.Author,
					"When will the event begin?\r\n" +
					"(Simply type a number of minutes from now like `10`" +
					" **OR** " +
					"if the event is today you can type a (CET/CEST) time like `20:00`)",
					triggerMessage.Channel,
					_promptTimeout
				)).Content;

				if (!TryParseEventTime(eventTime, out eventDurationMinutes))
				{
					await triggerMessage.Channel.SendMessageAsync(
						"I couldn't quite understand what time you meant. " +
						"Please tell me just a number of minutes like `10` or if the event is today a CET/CEST timestamp " +
						"like `14:00` or `06:00`."
					);
				}
			} while (eventDurationMinutes == null);

			var alertRole = await Prompt(
				triggerMessage.Author,
				"Name the role you want me to alert about the event (do not mention it with @, just give me the name)." +
				"Type `none` to skip.",
				triggerMessage.Channel,
				_promptTimeout
			);
			var role = Discord.GetRolesByName(alertRole.Content).FirstOrDefault();

			var hostMessageText = BuildHostMessage(
				role,
				eventName,
				triggerMessage,
				maxAttendees,
				eventDurationMinutes.Value,
				defaultAttendees
			);

			var hostMessage = await triggerMessage.Channel.SendMessageAsync(hostMessageText);
			if (defaultAttendees.Count >= maxAttendees)
			{
				await UpdateStatusAsync(hostMessage, "🔴", "Event is closed!");
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
					var eventStart = startOfDay
						.AddHours(hours)
						.AddMinutes(minutes);

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

		private string BuildHostMessage(
			SocketRole role,
			string eventName,
			IMessage triggerMessage,
			int maxAttendees,
			double eventDurationMinutes,
			IReadOnlyCollection<ulong> defaultAttendees
		)
		{
			var roleMention = role?.Mention != null
				? $" {role.Mention}"
				: "";

			var attendees = defaultAttendees.Any()
				? string.Join(" ", defaultAttendees.Select(uid => uid.MentionPerson()))
				: "No attendees yet";

			var hostMessageText = new StringBuilder();
			hostMessageText.AppendLine($"Hey{roleMention}, it's time for **{eventName}**!");
			hostMessageText.AppendLine();
			hostMessageText.AppendLine($"{triggerMessage.Author.Mention} is hosting a new event! {maxAttendees} people may join! React to this message with {_joinEmote} to join!");
			hostMessageText.AppendLine();
			hostMessageText.AppendLine($"🕒 {Duration.FromMinutes(eventDurationMinutes).ToString("HH:mm:ss", CultureInfo.InvariantCulture)}");
			hostMessageText.AppendLine($"🙋‍♂️ {attendees}");
			hostMessageText.AppendLine($"🟢 Event is now open");
			hostMessageText.AppendLine();
			hostMessageText.AppendLine("This message will automatically update.");

			return hostMessageText.ToString();
		}

		private async Task WatchForReactions(IUserMessage hostMessage, int maxAttendees, double eventDurationMinutes)
		{


			var expirationInstant = SystemClock.Instance.GetCurrentInstant().Plus(Duration.FromMinutes(eventDurationMinutes));

			Discord.ReactionAdded += ReactionHandler;
			Discord.ReactionRemoved += ReactionHandler;

			_jobScheduler.EveryHalfMinute += UpdateExpirationTimerAsync;

			_jobScheduler.Schedule(expirationInstant, async () =>
			{
				await EndWatchAsync();
			});

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

			async Task ReactionHandler(Cacheable<IUserMessage, ulong> message, Cacheable<IMessageChannel, ulong> channel, SocketReaction reaction)
				=> await HandleReaction(await message.GetOrDownloadAsync(), await channel.GetOrDownloadAsync(), reaction.User.Value, reaction.Emote);

			async Task HandleReaction(IUserMessage message, IMessageChannel channel, IUser user, IEmote reactionEmote)
			{
				if (user.IsBot)
				{
					return;
				}

				if (message.Id == hostMessage.Id)
				{
					_ = message.RemoveReactionAsync(reactionEmote, user.Id);
					var attendees = GetAttendees(hostMessage, maxAttendees);

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
			};

			async Task UpdateExpirationTimerAsync()
			{
				var attendees = GetAttendees(hostMessage, maxAttendees);

				await UpdateAttendeeListAsync(hostMessage, attendees);

				var remaining = expirationInstant - SystemClock.Instance.GetCurrentInstant();
				var countdownStart = hostMessage.Content.IndexOf("🕒");
				var countdownEnd = hostMessage.Content.IndexOfAfter("\n", "🕒");

				var updatedContent = hostMessage.Content.Splice(countdownStart, $"🕒 {remaining.ToString("HH:mm:ss", CultureInfo.InvariantCulture)}", countdownEnd);

				await hostMessage.ModifyAsync(msg => msg.Content = updatedContent);
			}

			async Task EndWatchAsync()
			{
				_jobScheduler.EveryHalfMinute -= UpdateExpirationTimerAsync;
				Discord.ReactionAdded -= ReactionHandler;
				Discord.ReactionRemoved -= ReactionHandler;
				await UpdateStatusAsync(hostMessage, "🔴", "Event is closed!");
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

		private HashSet<string> GetAttendees(IUserMessage hostMessage, int maxAttendees)
		{
			var attendees = hostMessage.Tags
				.Where(t => t.Type == TagType.UserMention)
				.Skip(1)
				.Select(t => t.Key)
				.ToList();

			return attendees.Select(uid => uid.MentionPerson()).ToHashSet();
		}

		private async Task UpdateAttendeeListAsync(IUserMessage hostMessage, HashSet<string> attendees)
		{
			var attendanceStart = hostMessage.Content.IndexOf("🙋‍♂️");
			var attendanceEnd = hostMessage.Content.IndexOfAfter("\n", "🙋‍♂️");

			var attendeesText = attendees.Count > 0 ? attendees.Join(" ") : "No attendees yet";

			var updatedContent = hostMessage.Content.Splice(attendanceStart, $"🙋‍♂️ {attendeesText}", attendanceEnd);

			await hostMessage.ModifyAsync(msg => msg.Content = updatedContent);
		}

		private async Task UpdateStatusAsync(IUserMessage hostMessage, string newEmoji, string statusText)
		{
			var statusEmoji = hostMessage.Content.IndexOf("🟢") == -1 ? "🔴" : "🟢";

			var statusStart = hostMessage.Content.IndexOf(statusEmoji);
			var statusEnd = hostMessage.Content.IndexOfAfter("\n", statusEmoji);

			var updatedContent = hostMessage.Content.Splice(statusStart, $"{newEmoji} {statusText}", statusEnd);

			await hostMessage.ModifyAsync(msg => msg.Content = updatedContent);
		}
	}
}
