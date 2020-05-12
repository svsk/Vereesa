using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using Discord;
using Discord.WebSocket;
using Vereesa.Core.Helpers;
using Vereesa.Core.Integrations.Interfaces;
using Vereesa.Data.Interfaces;
using Vereesa.Data.Models.Reminders;

namespace Vereesa.Core.Services
{
    public class RemindMeService : BotServiceBase
    {
        private IDiscordSocketClient _discord;
        private Timer _timer;
        private IRepository<Reminder> _reminderRepository;
        private string _commandWord = "!remindme";

        public RemindMeService(IDiscordSocketClient discord, IRepository<Reminder> reminderRepository)
        {
            _discord = discord;
            _discord.Ready -= InitializeAsync;
            _discord.Ready += InitializeAsync;
            _reminderRepository = reminderRepository;
        }

        private async Task InitializeAsync()
        {
            _timer?.Stop();
            _timer?.Dispose();
            _timer = await TimerHelpers.SetTimeoutAsync(HandleCheckIntervalElapsedAsync, 10000, true, true);
            _discord.MessageReceived -= HandleMessageReceived;
            _discord.MessageReceived += HandleMessageReceived;
        }

        private async Task HandleMessageReceived(SocketMessage receivedMessage)
        {
            if (TryParseReminder(receivedMessage, out var reminder))
            {
                await AddReminderAsync(reminder);
                await AnnounceReminderAddedAsync(receivedMessage.Channel.Id, receivedMessage.Author.Id);
            }
        }

        private async Task<IMessageChannel> GetChannelAsync(ulong channelId)
        {
            return (IMessageChannel)(await _discord.GetChannelAsync(channelId));
        }

        private async Task AnnounceReminderAddedAsync(ulong channelId, ulong reminderUser)
        {
            var channel = await GetChannelAsync(channelId);
            await channel.SendMessageAsync($"OK, <@{reminderUser}>! I'll remind you! :sparkles:");
        }

        private async Task AddReminderAsync(Reminder reminder)
        {
            await _reminderRepository.AddAsync(reminder);
        }

        private bool TryParseReminder(SocketMessage message, out Reminder reminder)
        {
            reminder = null;
            var msgContent = message.Content;

            if (msgContent.StartsWith(_commandWord) && msgContent.Contains("\""))
            {
                string reminderMessage = ExtractReminderMessage(msgContent);
                string reminderTime = ExtractReminderTime(msgContent, reminderMessage);
                bool reminderTimeParsed = TryParseReminderTime(reminderTime, out long remindUnixTimestamp);

                reminder = new Reminder();
                reminder.Message = reminderMessage;
                reminder.RemindTime = remindUnixTimestamp;
                reminder.UserId = message.Author.Id;
                reminder.ChannelId = message.Channel.Id;
            }

            return reminder != null;
        }

        private bool TryParseReminderTime(string reminderTime, out long remindUnixTimestamp)
        {
            remindUnixTimestamp = 0;

            try
            {
                var split = reminderTime.Split(" ");
                var number = int.Parse(split[0]);
                var unit = split[1].ToLowerInvariant();
                var now = DateTimeOffset.UtcNow;
                var remindTime = now;

                if (unit.Contains("second"))
                {
                    remindTime = now.AddSeconds(number);
                }
                else if (unit.Contains("minute"))
                {
                    remindTime = now.AddMinutes(number);
                }
                else if (unit.Contains("hour"))
                {
                    remindTime = now.AddHours(number);
                }
                else if (unit.Contains("day"))
                {
                    remindTime = now.AddDays(number);
                }
                else if (unit.Contains("month"))
                {
                    remindTime = now.AddMonths((int)number);
                }
                else if (unit.Contains("year"))
                {
                    remindTime = now.AddYears((int)number);
                }
                else
                {
                    throw new InvalidOperationException("Couldn't figure out time unit.");
                }

                remindUnixTimestamp = remindTime.ToUnixTimeSeconds();
            }
            catch
            {
                return false;
            }

            return true;
        }

        private string ExtractReminderMessage(string msgContent)
        {
            return msgContent.Split("\"").Skip(1).FirstOrDefault();
        }

        private string ExtractReminderTime(string msgContent, string reminderMessage)
        {
            return msgContent.Replace($"{_commandWord} ", string.Empty).Replace($"\"{reminderMessage}\"", string.Empty).ToLowerInvariant();
        }

        private async Task HandleCheckIntervalElapsedAsync()
        {
            var overdueReminders = await GetOverdueRemindersAsync();

            foreach (var reminder in overdueReminders)
            {
                if (await TryAnnounceReminderAsync(reminder))
                {
                    await RemoveReminderAsync(reminder);
                }
            }
        }

        private async Task RemoveReminderAsync(Reminder reminder)
        {
            await _reminderRepository.DeleteAsync(reminder);
        }

        private async Task<bool> TryAnnounceReminderAsync(Reminder reminder)
        {
            try
            {
                var channel = await GetChannelAsync(reminder.ChannelId);
                await channel.SendMessageAsync($"<@{reminder.UserId}> Remember! {reminder.Message}");
            }
            catch
            {
                return false;
            }

            return true;
        }

        private async Task<List<Reminder>> GetOverdueRemindersAsync()
        {
            return (await _reminderRepository.GetAllAsync()).Where(r => r.RemindTime < DateTimeOffset.UtcNow.ToUnixTimeSeconds()).ToList();
        }
    }
}