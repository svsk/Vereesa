using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using Vereesa.Core.Extensions;
using Vereesa.Data.Interfaces;
using Vereesa.Data.Models.Statistics;

namespace Vereesa.Core.Services
{
    public class FlagService
    {
        private readonly DiscordSocketClient _discord;
        private readonly IRepository<Statistics> _statRepository;
        private Statistics GetFlags() => _statRepository.FindById("flags") ?? new Statistics { Id = "flags" };
        private readonly ILogger<FlagService> _logger;

        public FlagService(DiscordSocketClient discord, IRepository<Statistics> statRepository, ILogger<FlagService> logger)
        {
            _discord = discord;
            _discord.MessageReceived += EvaluateMessageAsync;
            _statRepository = statRepository;
            _logger = logger;

            _logger.LogInformation($"{this.GetType().Name} loaded.");
        }

        private async Task EvaluateMessageAsync(SocketMessage message)
        {
            if (message?.GetCommand() == "!setflag") 
            {
                await message.Channel.SendMessageAsync(SetFlag(message.GetCommandArgs()));
            }
        }

        private string SetFlag(string[] args)
        {
            if (args == null || args.Length < 2) 
            {
                return "Please specify a country name and a flag emoji.";
            }

            var flag = args.Last();
            var country = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(string.Join(" ", args.Take(args.Length - 1)));
            
            var flags = GetFlags();
            flags.Upsert(country, flag);
            _statRepository.AddOrEdit(flags);

            return $"OK! {country} now has the flag {flag}!";
        }
    }
}