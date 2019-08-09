using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using Discord;
using Discord.WebSocket;
using Vereesa.Core.Extensions;
using Vereesa.Core.Helpers;
using Vereesa.Core.Integrations.Interfaces;
using Vereesa.Data.Interfaces;
using Vereesa.Data.Models.Reminders;

namespace Vereesa.Core.Services
{
    public class RemindMeService
    {
        private IDiscordSocketClient _discord;
        private Timer _timer;
        private IRepository<Reminder> _reminderRepository;
        private string _commandWord = "!remindme";

        public RemindMeService(IDiscordSocketClient discord, IRepository<Reminder> reminderRepository)
        {
            _discord = discord;
            _discord.Ready += Initialize;
            _reminderRepository = reminderRepository;
        }

        private async Task Initialize()
        {
            _timer?.Stop();
            _timer = TimerHelpers.SetTimeout(HandleCheckIntervalElapsed, 10000, true, true);
            _discord.MessageReceived += HandleMessageReceived;
        }

        private async Task HandleMessageReceived(SocketMessage receivedMessage)
        {
            if (TryParseReminder(receivedMessage, out var reminder))
            {
                AddReminder(reminder);
                AnnounceReminderAdded(receivedMessage.Channel.Id, receivedMessage.Author.Id);
            }
        }

        private IMessageChannel GetChannel(ulong channelId)
        {
            return (IMessageChannel)_discord.GetChannelAsync(channelId).GetAwaiter().GetResult();
        }

        private void AnnounceReminderAdded(ulong channelId, ulong reminderUser)
        {
            var channel = GetChannel(channelId);
            channel.SendMessageAsync($"OK, <@{reminderUser}>! I'll remind you! :sparkles:").GetAwaiter().GetResult();
        }

        private void AddReminder(Reminder reminder)
        {
            _reminderRepository.Add(reminder);
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

        private void HandleCheckIntervalElapsed()
        {
            var overdueReminders = GetOverdueReminders();

            foreach (var reminder in overdueReminders)
            {
                if (TryAnnounceReminder(reminder))
                {
                    RemoveReminder(reminder);
                }
            }
        }

        private void RemoveReminder(Reminder reminder)
        {
            _reminderRepository.Delete(reminder);
        }

        private bool TryAnnounceReminder(Reminder reminder)
        {
            try
            {
                var channel = GetChannel(reminder.ChannelId);
                channel.SendMessageAsync($"<@{reminder.UserId}> Remember! {reminder.Message}");
            }
            catch
            {
                return false;
            }

            return true;
        }

        private List<Reminder> GetOverdueReminders()
        {
            return _reminderRepository.GetAll().Where(r => r.RemindTime < DateTimeOffset.UtcNow.ToUnixTimeSeconds()).ToList();
        }
    }
}