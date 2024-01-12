using Discord;
using Microsoft.Extensions.Logging;
using Vereesa.Core;
using Vereesa.Core.Infrastructure;

namespace Vereesa.TestBot;

public class PingService : IBotService
{
    public PingService(ILogger<PingService> logger)
    {
        logger.LogInformation("Started PingService!");
    }

    [OnCommand("!ping")]
    public async Task Pong(IMessage message)
    {
        await message.Channel.SendMessageAsync("Pong!");
    }
}
