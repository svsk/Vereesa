using System.Collections.Generic;
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
	public class EventHostService : BotServiceBase
	{
		private static IEmote _joinEmote = new Emoji("✅");
		private readonly IJobScheduler _jobScheduler;

		public EventHostService(DiscordSocketClient discord, IJobScheduler jobScheduler) : base(discord)
		{
			_jobScheduler = jobScheduler;
		}

		[OnCommand("!host")]
		[AsyncHandler]
		[CommandUsage("Please only answer with a number when prompted for max attendees.")]
		public async Task CreateEventAsync(IMessage triggerMessage)
		{
			var eventName = (await Prompt(triggerMessage.Author, "What's the name of the event you're hosting?", triggerMessage.Channel)).Content;

			var maxAttendees = int.Parse((await Prompt(triggerMessage.Author, "How many people can attend?", triggerMessage.Channel)).Content);

			var eventDurationMinutes = int.Parse((await Prompt(triggerMessage.Author, "How many minutes is the event up for?", triggerMessage.Channel)).Content);

			var alertRole = await Prompt(triggerMessage.Author, "Name the role you want me to alert about the event (do not mention it with @, just give me the name). Type `none` to skip.", triggerMessage.Channel);
			var role = Discord.GetRolesByName(alertRole.Content).FirstOrDefault();

			var hostMessageText = BuildHostMessage(role, eventName, triggerMessage, maxAttendees, eventDurationMinutes);

			var hostMessage = await triggerMessage.Channel.SendMessageAsync(hostMessageText);

			await hostMessage.AddReactionAsync(_joinEmote);

			WatchForReactions(hostMessage, maxAttendees, eventDurationMinutes);
		}

		private string BuildHostMessage(SocketRole role, string eventName, IMessage triggerMessage, int maxAttendees, int eventDurationMinutes)
		{
			var hostMessageText = new StringBuilder();
			hostMessageText.AppendLine($"Hey {role?.Mention}, it's time for **{eventName}**!");
			hostMessageText.AppendLine();
			hostMessageText.AppendLine($"{triggerMessage.Author.Mention} is hosting a new event! {maxAttendees} people may join! React to this message with {_joinEmote} to join!");
			hostMessageText.AppendLine();
			hostMessageText.AppendLine($"🕒 {Duration.FromMinutes(eventDurationMinutes).ToString("HH:mm:ss", CultureInfo.InvariantCulture)}");
			hostMessageText.AppendLine($"🙋‍♂️ No attendees yet");
			hostMessageText.AppendLine($"🟢 Event is now open");
			hostMessageText.AppendLine();
			hostMessageText.AppendLine("This message will automatically update.");

			return hostMessageText.ToString();
		}

		private void WatchForReactions(IUserMessage hostMessage, int maxAttendees, int eventDurationMinutes)
		{
			var expirationInstant = SystemClock.Instance.GetCurrentInstant().Plus(Duration.FromMinutes(eventDurationMinutes));

			Discord.ReactionAdded += HandleReaction;
			Discord.ReactionRemoved += HandleReaction;

			_jobScheduler.EveryTenSeconds += UpdateExpirationTimerAsync;

			_jobScheduler.Schedule(expirationInstant, async () =>
			{
				await EndWatchAsync();
			});

			async Task HandleReaction(Cacheable<IUserMessage, ulong> message, ISocketMessageChannel channel, SocketReaction reaction)
			{
				if (reaction.MessageId == hostMessage.Id && reaction.Emote.Name == _joinEmote.Name)
				{
					var attendees = await GetAttendeesAsync(hostMessage, maxAttendees);

					await UpdateAttendeeListAsync(hostMessage, attendees);

					if (attendees.Count >= maxAttendees)
					{
						await EndWatchAsync();
					}
				}
			};

			async Task UpdateExpirationTimerAsync()
			{
				var attendees = await GetAttendeesAsync(hostMessage, maxAttendees);

				await UpdateAttendeeListAsync(hostMessage, attendees);

				var remaining = expirationInstant - SystemClock.Instance.GetCurrentInstant();
				var countdownStart = hostMessage.Content.IndexOf("🕒");
				var countdownEnd = hostMessage.Content.IndexOfAfter("\n", "🕒");

				var updatedContent = hostMessage.Content.Splice(countdownStart, $"🕒 {remaining.ToString("HH:mm:ss", CultureInfo.InvariantCulture)}", countdownEnd);

				await hostMessage.ModifyAsync(msg => msg.Content = updatedContent);
			}

			async Task UpdateStatusAsync(string newEmoji, string statusText)
			{
				var statusEmoji = hostMessage.Content.IndexOf("🟢") == -1 ? "🔴" : "🟢";

				var statusStart = hostMessage.Content.IndexOf(statusEmoji);
				var statusEnd = hostMessage.Content.IndexOfAfter("\n", statusEmoji);

				var updatedContent = hostMessage.Content.Splice(statusStart, $"{newEmoji} {statusText}", statusEnd);

				await hostMessage.ModifyAsync(msg => msg.Content = updatedContent);
			}

			async Task EndWatchAsync()
			{
				_jobScheduler.EveryTenSeconds -= UpdateExpirationTimerAsync;
				Discord.ReactionAdded -= HandleReaction;
				Discord.ReactionRemoved -= HandleReaction;
				await UpdateStatusAsync("🔴", "Event is closed!");
			}
		}

		private async Task<List<string>> GetAttendeesAsync(IUserMessage hostMessage, int maxAttendees)
		{
			var attendeeResult = await hostMessage.GetReactionUsersAsync(_joinEmote, maxAttendees + 1).ToListAsync();
			var attendees = attendeeResult.SelectMany(u => u).Where(u => !u.IsBot).Select(u => u.Mention).ToList();
			return attendees;
		}

		private async Task UpdateAttendeeListAsync(IUserMessage hostMessage, List<string> attendees)
		{
			var attendanceStart = hostMessage.Content.IndexOf("🙋‍♂️");
			var attendanceEnd = hostMessage.Content.IndexOfAfter("\n", "🙋‍♂️");

			var attendeesText = attendees.Count > 0 ? attendees.Join(" ") : "No attendees yet";

			var updatedContent = hostMessage.Content.Splice(attendanceStart, $"🙋‍♂️ {attendeesText}", attendanceEnd);

			await hostMessage.ModifyAsync(msg => msg.Content = updatedContent);
		}
	}
}