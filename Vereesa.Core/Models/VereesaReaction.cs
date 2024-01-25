using Discord;

namespace Vereesa.Core.Models;

public class VereesaReaction
{
    public IUser User { get; set; }
    public IEmote Emote { get; set; }
}
