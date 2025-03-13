using System.Text.Json;
using Vereesa.Neon.Data.Models;
using Vereesa.Neon.Data.Models.Wowhead;
using Vereesa.Neon.Integrations.Interfaces;

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

            var dragonflightEvents = todayInWow.FirstOrDefault(grp =>
                grp.Name == "Dragonflight" && grp.RegionId == "EU"
            );

            if (dragonflightEvents == null)
            {
                return null;
            }

            var elementalStormLines = dragonflightEvents
                ?.Groups.Where(grp => grp != null)
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
                    var stormClass = line
                        .Class?.Replace("elemental-storm-", string.Empty)
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

            var dragonflightEvents = todayInWow.FirstOrDefault(grp =>
                grp.Name == "Dragonflight" && grp.RegionId == "EU"
            );

            if (dragonflightEvents == null)
            {
                return null;
            }

            var grandHuntLines = dragonflightEvents
                .Groups?.Where(grp => grp != null)
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

        public async Task<List<RadiantEchoesEvent>?> GetCurrentRadiantEchoesEvents()
        {
            var todayInWow = await GetTodayInWow();

            var eventsInEu = todayInWow.FirstOrDefault(grp => grp.Id == "events-and-rares" && grp.RegionId == "EU");
            var radiantEchoesTimer = eventsInEu
                ?.Groups.Where(grp => grp != null)
                .FirstOrDefault(grp => grp.Id == "radiant-echoes")
                ?.Content;

            if (radiantEchoesTimer == null)
            {
                return null;
            }

            var trackedEchoes = radiantEchoesTimer
                .Upcoming.Select(
                    (unixStartTime, idx) =>
                    {
                        var label = radiantEchoesTimer.UpcomingLabels[idx];

                        if (!WoWZoneHelper.TryParseWoWZone(label, out var zone))
                        {
                            return null;
                        }

                        var unixEndTime = radiantEchoesTimer.Upcoming.ElementAtOrDefault(idx + 1);

                        var startTime = DateTimeOffset.FromUnixTimeSeconds(unixStartTime);
                        var endTime =
                            unixEndTime > 0 ? DateTimeOffset.FromUnixTimeSeconds(unixEndTime) : (DateTimeOffset?)null;

                        return new RadiantEchoesEvent
                        {
                            ZoneId = zone,
                            StartedAt = startTime,
                            EndingAt = endTime,
                        };
                    }
                )
                .OfType<RadiantEchoesEvent>()
                .ToList();

            return trackedEchoes;
        }

        public async Task<TodayInWowSection[]> GetTodayInWow()
        {
            try
            {
                var result = await _httpClient.GetAsync("https://wwww.wowhead.com/");

                var html = await result.Content.ReadAsStringAsync();

                var start = html.IndexOf("id=\"data.wow.todayInWow\">");
                var end = html.IndexOf("</", start);
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
