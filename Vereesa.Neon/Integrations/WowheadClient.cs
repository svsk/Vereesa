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

        public async Task<ElementalStorm> GetCurrentElementalStorm()
        {
            var todayInWow = await GetTodayInWow();

            var eventsInEu = todayInWow.FirstOrDefault(grp => grp.Id == "events-and-rares" && grp.RegionId == "EU");
            var currentElementalStorm = eventsInEu?.Groups
                .Where(grp => grp != null)
                .FirstOrDefault(grp => grp.Id == "elemental-storms")
                ?.Content.Lines.First();

            var name = currentElementalStorm?.Name;
            var stormClass = currentElementalStorm
                ?.Class?.Replace("elemental-storm-", string.Empty)
                .Replace("elemental-storm", string.Empty);

            var zone = name != "Upcoming" && name != "In Progress" ? name : null;

            var type = stormClass switch
            {
                "water" => ElementalStormType.Water,
                "earth" => ElementalStormType.Earth,
                "fire" => ElementalStormType.Fire,
                "air" => ElementalStormType.Air,
                _ => ElementalStormType.Unknown,
            };

            WoWZone? wowZone = null;
            WoWZoneHelper.TryParseWoWZone(zone, out wowZone);

            return new ElementalStorm
            {
                Type = type,
                ZoneId = wowZone,
                Status = zone != null ? "Active" : "Upcoming",
                Time = DateTimeOffset.FromUnixTimeSeconds(currentElementalStorm?.EndingUt ?? 0),
            };
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
