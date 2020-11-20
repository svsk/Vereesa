using Discord.WebSocket;
using Vereesa.Core.Infrastructure;

namespace Vereesa.Core.Services
{
	public class BlueTrackerService : BotServiceBase
	{
		public BlueTrackerService(DiscordSocketClient discord)
			: base(discord)
		{
		}
	}
}