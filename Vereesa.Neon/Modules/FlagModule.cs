using System.ComponentModel;
using Discord;
using Vereesa.Core;
using Vereesa.Core.Infrastructure;
using Vereesa.Neon.Services;

namespace Vereesa.Neon.Modules;

public class FlagModule : IBotModule
{
    private readonly FlagService _flagService;

    public FlagModule(FlagService flagService) => _flagService = flagService;

    [OnCommand("!flag set")]
    [WithArgument("countryName", 0)]
    [WithArgument("flagEmoji", 1)]
    [Authorize("Guild Master")]
    [Description("Sets a flag for the specified country.")]
    [CommandUsage("`!flag set <flag emoji> <country name>`")]
    public async Task EvaluateMessageAsync(IMessage message, string flagEmoji, string countryName) =>
        await message.Channel.SendMessageAsync(_flagService.SetFlag(flagEmoji, countryName));
}
