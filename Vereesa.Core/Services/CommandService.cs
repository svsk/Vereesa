using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Vereesa.Core.Extensions;
using Vereesa.Data.Models.Commands;
using Vereesa.Data.Repositories;

namespace Vereesa.Core.Services
{
    public class CommandService
    {
        private DiscordSocketClient _discord;
        private JsonRepository<Command> _commandRepo;

        public CommandService(DiscordSocketClient discord, JsonRepository<Command> commandRepo)
        {
            _discord = discord;
            _commandRepo = commandRepo;

            _discord.MessageReceived += CheckMessage;
        }

        private async Task CheckMessage(SocketMessage srcMessage)
        {            
            var command = srcMessage.GetCommand();

            if (command == "!addcmd" && srcMessage.Author.Username == "Veinlash")
            {
                TryAddCommand(srcMessage);
            }

            if (command.StartsWith("!"))
            {
                await TryTriggerCommand(command, srcMessage.Channel);
            }
        }

        private void TryAddCommand(SocketMessage srcMessage)
        {
            var parameters = srcMessage.GetCommandArgs();

            var trigger = parameters[0];
            var type = parameters[1];
            var returnMessage = string.Join(" ", parameters.Skip(2));

            trigger = trigger.StartsWith("!") ? trigger : "!" + trigger;

            _commandRepo.Add(new Command
            {
                Id = Guid.NewGuid().ToString(),
                TriggerCommands = new List<string> { trigger },
                CommandType = CommandTypeEnum.Countdown,
                ReturnMessage = returnMessage
            });

            _commandRepo.Save();
        }

        private async Task TryTriggerCommand(string command, IMessageChannel responseChannel)
        {
            var triggeredCommands = _commandRepo.GetAll().Where(cmd => cmd.TriggerCommands.Contains(command));


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
                                
                            returnMessage = returnMessage.Replace(tag.ReplacePattern, $"{ts.Value.Days} days, {ts.Value.Hours} hours, {ts.Value.Minutes} minutes, and {ts.Value.Seconds} seconds until");
                        }

                        break;
                }

                await responseChannel.SendMessageAsync(returnMessage);
            }
        }

        public static IEnumerable<T> GetTagsByType<T>(string sourceString) where T : TagBase, new()
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
                result = parsedResult - DateTime.Now.ToCentralEuropeanTime();
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