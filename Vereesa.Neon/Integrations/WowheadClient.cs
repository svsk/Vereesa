using Vereesa.Neon.Integrations.Interfaces;
using Vereesa.Neon.Data.Models.Wowhead;
using System.Text.Json;
using Vereesa.Neon.Data.Models;

namespace Vereesa.Neon.Integrations
{
    public class WowheadClient : IWowheadClient
    {
        private readonly HttpClient _httpClient;

        public WowheadClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<ElementalStorm>?> GetCurrentElementalStorms()
        {
            var todayInWow = await GetTodayInWow();

            var eventsInEu = todayInWow.FirstOrDefault(grp => grp.Id == "events-and-rares" && grp.RegionId == "EU");
            var elementalStormLines = eventsInEu?.Groups
                .Where(grp => grp != null)
                .FirstOrDefault(grp => grp.Id == "elemental-storms")
                ?.Content.Lines;

            if (elementalStormLines == null)
            {
                return null;
            }

            var storms = elementalStormLines
                .Where(line => line != null)
                .Select(line =>
                {
                    var stormClass = line.Class
                        ?.Replace("elemental-storm-", string.Empty)
                        .Replace("elemental-storm", string.Empty);

                    var name = line.Name;
                    var zone = name != "Upcoming" && name != "In Progress" ? name : null;

                    var type = stormClass switch
                    {
                        "water" => ElementalStormType.Snowstorm,
                        "earth" => ElementalStormType.Sandstorm,
                        "fire" => ElementalStormType.Firestorm,
                        "air" => ElementalStormType.Thunderstorm,
                        _ => ElementalStormType.Unknown,
                    };

                    WoWZone? wowZone = null;
                    WoWZoneHelper.TryParseWoWZone(zone, out wowZone);

                    return new ElementalStorm
                    {
                        Type = type,
                        ZoneId = wowZone,
                        Status = zone != null ? "Active" : "Upcoming",
                        Time = DateTimeOffset.FromUnixTimeSeconds(line.EndingUt ?? 0),
                    };
                })
                .ToList();

            var activeStorms = storms.Where(storm => storm.Status == "Active").ToList();
            var upcomingStorms = storms.Where(storm => storm.Status == "Upcoming").ToList();

            // If there are any active storms, return those. Otherwise, return the first upcoming storm.
            return activeStorms.Any() ? activeStorms : upcomingStorms.Take(1).ToList();
        }

        public async Task<List<GrandHunt>?> GetCurrentGrandHunts()
        {
            var todayInWow = await GetTodayInWow();

            var eventsInEu = todayInWow.FirstOrDefault(grp => grp.Id == "events-and-rares" && grp.RegionId == "EU");
            var grandHuntLines = eventsInEu?.Groups
                .Where(grp => grp != null)
                .FirstOrDefault(grp => grp.Id == "grand-hunt")
                ?.Content.Lines;

            if (grandHuntLines == null)
            {
                return null;
            }

            return grandHuntLines
                .Where(line => line != null)
                .Select(line =>
                {
                    WoWZoneHelper.TryParseWoWZone(line.Name, out var zone);

                    return new GrandHunt
                    {
                        ZoneId = zone,
                        Time = DateTimeOffset.FromUnixTimeSeconds(line.EndingUt ?? 0),
                    };
                })
                .ToList();
        }

        public async Task<TodayInWowSection[]> GetTodayInWow()
        {
            try
            {
                var result = await _httpClient.GetAsync("https://wwww.wowhead.com/");

                var html = await result.Content.ReadAsStringAsync();

                var start = html.IndexOf("new WH.Wow.TodayInWow(");
                var end = html.IndexOf(");", start);
                var arrayStart = html.IndexOf("[", start);
                var json = html.Substring(arrayStart, end - arrayStart);

                // Ignore null values in collections.
                // Specialized converter for handling arrays that are suppoed to be objects.
                var todayInWow = JsonSerializer.Deserialize<TodayInWowSection[]>(
                    json,
                    new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        Converters = { new TodayInWowSectionGroupContentLineConverter() },
                    }
                );

                if (todayInWow == null)
                {
                    throw new Exception("Failed to parse TodayInWow JSON");
                }

                return todayInWow;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            return new TodayInWowSection[0];
        }
    }
}
