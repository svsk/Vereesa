using System.Text.RegularExpressions;
using Discord;
using Vereesa.Core.Extensions;
using Vereesa.Neon.Extensions;
using Vereesa.Neon.Data.Interfaces;
using Vereesa.Neon.Data.Models.Commands;
using Vereesa.Core.Infrastructure;
using System.ComponentModel;
using Vereesa.Core;

namespace Vereesa.Neon.Services
{
    public class CommandService : IBotService
    {
        private IRepository<Command> _commandRepo;

        public CommandService(IRepository<Command> commandRepo)
        {
            _commandRepo = commandRepo;
        }

        [OnCommand("!addcmd")]
        [Description("Adds a command to Vereesa. Only available to Guild Master role.")]
        [Authorize("Guild Master")]
        [AsyncHandler]
        public async Task CheckMessage(IMessage srcMessage)
        {
            await TryAddCommandAsync(srcMessage);
        }

        [OnMessage]
        public async Task HandleCustomCommand(IMessage srcMessage)
        {
            var command = srcMessage.GetCommand();

            if (command != null && command.StartsWith("!"))
            {
                await TryTriggerCommandAsync(command, srcMessage.Channel);
            }
        }

        private async Task TryAddCommandAsync(IMessage srcMessage)
        {
            var parameters = srcMessage.GetCommandArgs();

            var trigger = parameters[0];
            var type = parameters[1];
            var returnMessage = string.Join(" ", parameters.Skip(2));

            trigger = trigger.StartsWith("!") ? trigger : "!" + trigger;

            await _commandRepo.AddAsync(
                new Command
                {
                    Id = Guid.NewGuid().ToString(),
                    TriggerCommands = new List<string> { trigger },
                    CommandType = CommandTypeEnum.Countdown,
                    ReturnMessage = returnMessage
                }
            );

            await _commandRepo.SaveAsync();
        }

        private async Task TryTriggerCommandAsync(string command, IMessageChannel responseChannel)
        {
            var triggeredCommands = (await _commandRepo.GetAllAsync()).Where(
                cmd => cmd.TriggerCommands.Contains(command)
            );

            if (triggeredCommands.Any())
            {
                var triggeredCommand = triggeredCommands.First();
                var returnMessage = triggeredCommand.ReturnMessage;

                switch (triggeredCommand.CommandType)
                {
                    case CommandTypeEnum.Countdown:
                        var tags = GetTagsByType<TimeUntilTag>(returnMessage);

                        foreach (var tag in tags)
                        {
                            var ts = tag.GetTimeUntilDate();
                            if (ts == null)
                                continue;

                            if (ts.Value.TotalSeconds < 0)
                                ts = new TimeSpan();

                            returnMessage = returnMessage.Replace(
                                tag.ReplacePattern,
                                $"{ts.Value.Days} days, {ts.Value.Hours} hours, {ts.Value.Minutes} minutes, and {ts.Value.Seconds} seconds until"
                            );
                        }

                        break;
                }

                await responseChannel.SendMessageAsync(returnMessage);
            }
        }

        public static IEnumerable<T> GetTagsByType<T>(string sourceString)
            where T : TagBase, new()
        {
            var identifier = new T().TagIdentifier;
            var regex = new Regex("{" + identifier + ":(.*?)}");
            var matchResult = regex.Matches(sourceString);

            var tags = new List<T>();

            foreach (var match in matchResult)
            {
                var tag = new T();
                tag.ReplacePattern = ((Match)match).Groups[0].ToString();
                tag.TagValue = ((Match)match).Groups[1].ToString();
                tags.Add(tag);
            }

            return tags;
        }
    }

    public class TimeUntilTag : TagBase
    {
        public override string TagIdentifier => "timeUntil";

        public TimeSpan? GetTimeUntilDate()
        {
            TimeSpan? result = null;

            if (DateTime.TryParse(this.TagValue, out var parsedResult))
            {
                result = parsedResult - DateTimeExtensions.NowInCentralEuropeanTime();
            }

            return result;
        }
    }

    public class TagBase
    {
        public virtual string TagIdentifier => "tag";
        public string ReplacePattern { get; set; }
        public string TagValue { get; set; }
    }
}
