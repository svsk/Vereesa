using System.Threading.Tasks;
using Discord;
using Vereesa.Core.Infrastructure;

namespace Vereesa.Core.Services;

public class IntroductionService : IBotService
{
    private readonly IMessagingClient _messaging;

    public IntroductionService(IMessagingClient messaging)
    {
        _messaging = messaging;
    }

    [OnCommand("!introduce")]
    [AsyncHandler]
    public async Task HandleMessage(IMessage message)
    {
        var age = await _messaging.Prompt(message.Author, "How old are you?", message.Channel, 60000);
        var nationality = await _messaging.Prompt(message.Author, "Where are you from?", message.Channel, 60000);
        var firstName = await _messaging.Prompt(message.Author, "What is your first name?", message.Channel, 60000);
        var joinedNeon = await _messaging.Prompt(
            message.Author,
            "When did you first join Neon?",
            message.Channel,
            60000
        );

        var startedWow = await _messaging.Prompt(
            message.Author,
            "When did you start playing WoW?",
            message.Channel,
            60000
        );

        var mainCharName = await _messaging.Prompt(
            message.Author,
            "What is the name of your main character?",
            message.Channel,
            60000
        );

        var mainCharClass = await _messaging.Prompt(
            message.Author,
            "What class is your main character?",
            message.Channel,
            60000
        );
    }
}
