using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Vereesa.Neon.Data.Models;

public enum WoWZone
{
    [Display(Name = "The Waking Shores")]
    TheWakingShores = 13644,

    [Display(Name = "Ohn'ahran Plains")]
    OhnAhranPlains = 13645,

    [Display(Name = "The Azure Span")]
    TheAzureSpan = 13646,

    [Display(Name = "Thaldraszus")]
    Thaldraszus = 13647,
}

public static class WoWZoneHelper
{
    public static string GetName(WoWZone wowZone)
    {
        var displayAttribute =
            wowZone
                .GetType()
                .GetField(wowZone.ToString())
                .GetCustomAttributes(typeof(DisplayAttribute), false)
                .FirstOrDefault() as DisplayAttribute;

        return displayAttribute?.Name ?? wowZone.ToString();
    }

    public static bool TryParseWoWZone(string zoneNameToParse, out WoWZone? zone)
    {
        zone = null;

        if (string.IsNullOrEmpty(zoneNameToParse))
        {
            return false;
        }

        if (zoneNameToParse.StartsWith("The ", StringComparison.OrdinalIgnoreCase))
        {
            zoneNameToParse = zoneNameToParse.Substring(4);
        }

        var enumEntries = Enum.GetValues(typeof(WoWZone));

        foreach (WoWZone entry in enumEntries)
        {
            var displayAttribute =
                entry
                    .GetType()
                    .GetField(entry.ToString())
                    .GetCustomAttributes(typeof(DisplayAttribute), false)
                    .FirstOrDefault() as DisplayAttribute;

            if (displayAttribute == null)
            {
                continue;
            }

            var entryName = displayAttribute.Name;

            if (entryName.StartsWith("The ", StringComparison.OrdinalIgnoreCase))
            {
                entryName = entryName.Substring(4);
            }

            if (displayAttribute != null && zoneNameToParse.Equals(entryName, StringComparison.OrdinalIgnoreCase))
            {
                zone = entry;
                return true;
            }
        }

        return false;
    }
}
