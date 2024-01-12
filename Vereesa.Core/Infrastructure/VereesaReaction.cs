using Discord;

namespace Vereesa.Core.Infrastructure;

public class VereesaReaction
{
    public IUser User { get; set; }
    public IEmote Emote { get; set; }
}
