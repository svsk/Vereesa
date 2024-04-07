using System;

namespace Vereesa.Neon.Data.Models.Wowhead;

public class GrandHunt
{
    public WoWZone? ZoneId { get; set; }
    public string Zone => ZoneId != null ? WoWZoneHelper.GetName(ZoneId.Value) : null;
    public DateTimeOffset Time { get; set; }

    public DateTimeOffset EndingAt => Time;
    public DateTimeOffset StartedAt => Time.AddHours(-2);
}
