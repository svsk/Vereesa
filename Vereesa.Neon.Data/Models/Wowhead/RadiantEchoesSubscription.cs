using System;
using Vereesa.Neon.Data.Interfaces;

namespace Vereesa.Neon.Data.Models.Wowhead;

public class RadiantEchoesSubscription : IEntity
{
    public string Id { get; set; }
    public ulong UserId { get; set; }
    public WoWZone Zone { get; set; }
    public DateTimeOffset? LastNotifiedAt { get; set; }
}
