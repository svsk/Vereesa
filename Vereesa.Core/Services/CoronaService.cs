using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RestSharp;
using Vereesa.Core.Extensions;
using Vereesa.Data.Interfaces;
using Vereesa.Data.Models.Statistics;

namespace Vereesa.Core.Services
{
    public class CoronaService
    {
        private DiscordSocketClient _discord;
        private IRepository<Statistics> _statRepository;
        private ILogger<CoronaService> _logger;
        private Statistics _coronaStats => _statRepository.FindById("corona") ?? new Statistics { Id = "corona" };
        private Statistics _flags => _statRepository.FindById("flags") ?? new Statistics { Id = "flags" };

        public CoronaService(DiscordSocketClient discord, IRepository<Statistics> statRepository, ILogger<CoronaService> logger)
        {
            _discord = discord;
            _discord.MessageReceived += EvaluateMessageAsync;
            _statRepository = statRepository;
            _logger = logger;

            _logger.LogInformation($"{this.GetType().Name} loaded.");
        }

        private async Task EvaluateMessageAsync(SocketMessage message)
        {
            var command = message?.GetCommand();
            
            if (command == null)
            {
                return;
            }
            
            if (command == "!corona") 
            {
                await message.Channel.SendMessageAsync(GetCoronaReport());
            }

            if (command == "!setneoninfected") 
            {
                await message.Channel.SendMessageAsync(SetGuildInfected(message.GetCommandArgs()));
            }

            if (command == "!addcoronacountry") 
            {
                await message.Channel.SendMessageAsync(AddRelevantCountry(message.GetCommandArgs()));
            }

            if (command == "!removecoronacountry") 
            {
                await message.Channel.SendMessageAsync(RemoveRelevantCountry(message.GetCommandArgs()));
            }
        }

        private string AddRelevantCountry(string[] countryWords)
        {
            if (countryWords == null || !countryWords.Any()) 
            {
                return "Please specify a country";
            }

            var country = string.Join(" ", countryWords);
            var countryStats = GetCountryStats(country);
            if (countryStats == null) 
            {
                return "Couldn't find that country in my stats. Please ensure that you spelt it correctly and that it has Title Casing.";
            }

            var existingCountries = GetRelevantCountries();

            if (existingCountries.Contains(country)) 
            {
                return "I already have that country on my watch list.";
            }

            SaveRelevantCountry(country);

            return $"Added {country} to the watch list.";
        }

        private string RemoveRelevantCountry(string[] countryWords)
        {
            if (countryWords == null || !countryWords.Any()) 
            {
                return "Please specify a country";
            }

            var country = string.Join(" ", countryWords);
            var existingCountries = GetRelevantCountries();
            if (!existingCountries.Contains(country)) 
            {
                return "I don't have that country on my watch list.";
            }

            DeleteRelevantCountry(country);

            return $"Removed {country} from the watch list.";
        }

        private string SetGuildInfected(string[] commandArgs)
        {
            if (!int.TryParse(commandArgs.FirstOrDefault(), out var numberOfInfected)) 
            {
                return "I don't get what you mean. You need to add a number to your command.";
            }

            UpdateInfectedStat(numberOfInfected);

            return $"There are now {numberOfInfected} registered cases of Neon members infected with Corona virus.";
        }

        private string GetCoronaReport()
        {
            return $"**:rotating_light: Corona virus pandemic is ongoing :rotating_light:** {GenerateRelevantCountryReport()}";
        }

        private CoronaCountryStats GetCountryStats(string countryName) 
        {
            var restClient = new RestClient("https://corona.lmao.ninja/countries/");
            var restRequest = new RestRequest(countryName, Method.GET);

            var response = restClient.Execute(restRequest);

            if (!response.IsSuccessful) 
            {
                return null;
            }

            try 
            {
                return JsonConvert.DeserializeObject<CoronaCountryStats>(response.Content);
            }
            catch 
            {
                return null;
            }
        }

        private string GenerateRelevantCountryReport() 
        {
            var relevantCountries = GetRelevantCountries();
            var flags = _flags;

            var sb = new StringBuilder();
            sb.AppendLine();

            void AppendRow(StringBuilder builder, CoronaCountryStats countryStats) 
            {
                var flag = flags.Get<string>(countryStats.Name);

                builder.AppendLine($"**{(flag == null ? "" : flag + " ")}{countryStats.Name}**");
                builder.AppendLine($"Confirmed cases: {countryStats.Cases} (+{countryStats.TodayCases})");
                builder.AppendLine($"Deaths: {countryStats.Deaths} (+{countryStats.TodayDeaths})");
                builder.AppendLine($"Recovered: {countryStats.Recovered}");
                builder.AppendLine();
            };

            foreach (var country in relevantCountries.Distinct()) 
            {
                var stats = GetCountryStats(country);
                if (stats == null)
                    continue;

                AppendRow(sb, stats);
            }

            var neonInfected = GetInfectedStat() ?? 0;
            AppendRow(sb, new CoronaCountryStats {
                Name = "Neon",
                Cases = neonInfected,
                TodayCases = 0,
                Deaths = 0,
                TodayDeaths = 0,
                Recovered = 0
            });
            
            return sb.ToString();
        }

        private List<string> GetRelevantCountries()
        {
            return _coronaStats.Get<List<string>>("countries") ?? new List<string>();
        }

        private void SaveRelevantCountry(string country) 
        {
            var stats = _coronaStats;
            var targetKey = "countries";

            var countries = GetRelevantCountries();
            countries.Add(country);
            stats.Upsert(targetKey, countries);

            _statRepository.Add(stats);
        }

        private void DeleteRelevantCountry(string country) 
        {
            var stats = _coronaStats;
            var targetKey = "countries";

            var countries = GetRelevantCountries();
            countries.Remove(country);
            stats.Upsert(targetKey, countries);

            _statRepository.Add(stats);
        }

        private int? GetInfectedStat() 
        {
            try 
            {
                return _coronaStats.Get<int>("guildInfected");
            } 
            catch 
            {
                return null;
            }
        }

        private void UpdateInfectedStat(int numberOfInfected) 
        {
            var stats = _coronaStats;
            var targetKey = "guildInfected";

            if (stats.Stats.ContainsKey(targetKey)) 
            {
                stats.Stats[targetKey] = numberOfInfected;
            } 
            else 
            {
                stats.Stats.Add(targetKey, numberOfInfected);
            }

            _statRepository.Add(stats);
        }

        private class CoronaCountryStats 
        {
            [JsonProperty("country")]
            public string Name { get; set; }


            [JsonProperty("cases")] // 5560,
            public int? Cases { get; set; }

            [JsonProperty("todayCases")] // 811,
            public int? TodayCases { get; set; }

            [JsonProperty("deaths")] // 276,
            public int? Deaths { get; set; }

            [JsonProperty("todayDeaths")] // 63,
            public int? TodayDeaths { get; set; }

            [JsonProperty("recovered")] // 2,
            public int? Recovered { get; set; }

            [JsonProperty("active")] // 5282,
            public int? Active { get; set; }

            [JsonProperty("critical")] // 435,
            public int? Critical { get; set; }

            [JsonProperty("casesPerOneMillion")] // 324,
            public int? CasesPerOneMillion { get; set; }

            [JsonProperty("deathsPerOneMillion")] // 16
            public int? DeathsPerOneMillion { get; set; }


        }

        private class CoronaCountryTimeEntry 
        {
            
            
            [JsonProperty("confirmed")]
            public int? Confirmed { get; set; }
            
            [JsonProperty("deaths")]
            public int? Deaths { get; set; }
            
            [JsonProperty("recovered")]
            public int? Recovered { get; set; }
        }
    }
}