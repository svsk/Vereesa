using System.Linq;
using System.Threading.Tasks;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using Vereesa.Core.Extensions;
using Vereesa.Data.Interfaces;
using Vereesa.Data.Models.Statistics;
using Vereesa.Core.Infrastructure;
using System.ComponentModel;

namespace Vereesa.Core.Services
{
	public class FlagService : BotServiceBase
	{
		private readonly IRepository<Statistics> _statRepository;
		private Statistics GetFlags() => _statRepository.FindById("flags") ??
			new Statistics { Id = "flags" };
		private readonly ILogger<FlagService> _logger;

		public FlagService(DiscordSocketClient discord, IRepository<Statistics> statRepository,
			ILogger<FlagService> logger)
			: base(discord)
		{
			_statRepository = statRepository;
			_logger = logger;
		}

		[OnCommand("!setflag")]
		[Authorize("Guild Master")]
		[Description("Sets a flag for the specified country.")]
		[CommandUsage("`!setflag <country name> <flag emoji>`")]
		private async Task EvaluateMessageAsync(SocketMessage message) =>
			await message.Channel.SendMessageAsync(SetFlag(message.GetCommandArgs()));

		private string SetFlag(string[] args)
		{
			if (args == null || args.Length < 2)
			{
				return "Please specify a country name and a flag emoji.";
			}

			var flag = args.Last();
			var country = string.Join(" ", args.Take(args.Length - 1)).ToTitleCase();

			var flags = GetFlags();
			flags.Upsert(country, flag);
			_statRepository.AddOrEdit(flags);

			return $"OK! {country} now has the flag {flag}!";
		}
	}
}