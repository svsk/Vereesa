using System;

namespace Vereesa.Neon.Data.Models.Wowhead;

public enum ElementalStormType
{
    Snowstorm,
    Sandstorm,
    Firestorm,
    Thunderstorm,
    Unknown,
}

public class ElementalStorm
{
    public ElementalStormType? Type { get; set; }
    public string ElementType =>
        Type switch
        {
            ElementalStormType.Thunderstorm => "air",
            ElementalStormType.Sandstorm => "earth",
            ElementalStormType.Firestorm => "fire",
            ElementalStormType.Snowstorm => "water",
            _ => "Unknown"
        };

    public WoWZone? ZoneId { get; set; }
    public string Zone => ZoneId != null ? WoWZoneHelper.GetName(ZoneId.Value) : null;
    public string Status { get; set; }
    public DateTimeOffset Time { get; set; }
    public DateTimeOffset EndingAt => Status == "Active" ? Time : Time.AddHours(2);
    public DateTimeOffset StartingAt => Status == "Active" ? Time.AddHours(-2) : Time;
    public string IconUrl =>
        Type == null
            ? null
            : $"https://wow.zamimg.com/images/wow/TextureAtlas/live/elementalstorm-lesser-{ElementType}.webp";
}
