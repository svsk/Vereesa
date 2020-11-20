using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Vereesa.Core.Infrastructure;

namespace Vereesa.Core.Services
{
	public class HelpService : BotServiceBase
	{
		public HelpService(DiscordSocketClient discord) : base(discord) { }

		[OnCommand("!help")]
		public async Task HandleMessage(IMessage message)
		{
			// print help
		}
	}
}