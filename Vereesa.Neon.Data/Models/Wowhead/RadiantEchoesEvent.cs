using System;

namespace Vereesa.Neon.Data.Models.Wowhead;

public class RadiantEchoesEvent
{
    public WoWZone? ZoneId { get; set; }
    public string Zone => ZoneId != null ? WoWZoneHelper.GetName(ZoneId.Value) : null;

    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? EndingAt { get; set; }
}
