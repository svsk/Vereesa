using Discord.WebSocket;

namespace Vereesa.Core.Services
{
    public class BotServiceBase
    {
        // Inheriting this class auto starts it on in the VereesaClient.cs
		protected DiscordSocketClient Discord { get; }

		public BotServiceBase(DiscordSocketClient discord)
        {
            Discord = discord;
        }
	}
}