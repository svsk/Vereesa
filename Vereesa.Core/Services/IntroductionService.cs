using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Vereesa.Core.Infrastructure;

namespace Vereesa.Core.Services;

public class IntroductionService : BotServiceBase
{
	public IntroductionService(DiscordSocketClient discord) : base(discord) { }

	[OnCommand("!introduce")]
	[AsyncHandler]
	public async Task HandleMessage(IMessage message)
	{
		var age = await Prompt(message.Author, "How old are you?", message.Channel, 60000);
		var nationality = await Prompt(message.Author, "Where are you from?", message.Channel, 60000);
		var firstName = await Prompt(message.Author, "What is your first name?", message.Channel, 60000);
		var joinedNeon = await Prompt(message.Author, "When did you first join Neon?", message.Channel, 60000);
		var startedWow = await Prompt(message.Author, "When did you start playing WoW?", message.Channel, 60000);
		var mainCharName = await Prompt(message.Author, "What is the name of your main character?", message.Channel, 60000);
		var mainCharClass = await Prompt(message.Author, "What class is your main character?", message.Channel, 60000);


	}
}

