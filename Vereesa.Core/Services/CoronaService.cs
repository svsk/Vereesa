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

            if (command == "!listcoronacountries") 
            {
                await message.Channel.SendMessageAsync(ListCoronaCountries());
            }
        }

        private string ListCoronaCountries()
        {
            var worldStats = GetCoronaWorldStats();
            var sb = new StringBuilder();
            foreach (var item in worldStats.Keys.OrderBy(c => c)) 
            {
                sb.AppendLine(item);
            }

            return sb.ToString();
        }

        private string AddRelevantCountry(string[] countryWords)
        {
            if (countryWords == null || !countryWords.Any()) 
            {
                return "Please specify a country";
            }

            var country = string.Join(" ", countryWords);
            var countries = GetCoronaWorldStats();
            if (!countries.ContainsKey(country)) 
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
            var neonInfected = GetInfectedStat() ?? 0;
            return $"**:rotating_light: Corona virus pandemic is ongoing :rotating_light:** {GenerateRelevantCountryReport()} There {(neonInfected == 1 ? "is" : "are")} {neonInfected} Neon member{(neonInfected == 1 ? "" : "s")} infected.";
        }

        private Dictionary<string, List<CoronaCountryTimeEntry>> GetCoronaWorldStats() 
        {
            var restClient = new RestClient("https://pomber.github.io"); 
            var restRequest = new RestRequest("/covid19/timeseries.json", Method.GET);
            var response = restClient.Execute(restRequest);
            var countries = JsonConvert.DeserializeObject<Dictionary<string, List<CoronaCountryTimeEntry>>>(response.Content);
            return countries;
        }

        private string GenerateRelevantCountryReport() 
        {
            var relevantCountries = GetRelevantCountries();
            var countries = GetCoronaWorldStats();
            var flags = _flags;

            var sb = new StringBuilder();
            sb.AppendLine();

            foreach (var country in relevantCountries.Distinct()) 
            {
                var lastTwo = countries[country].TakeLast(2);
                var previous = lastTwo.First();
                var current = lastTwo.Last();

                var confirmedChange = current.Confirmed - previous.Confirmed;
                var deathsChange = current.Deaths - previous.Deaths;
                var recoveredChange = current.Recovered - previous.Recovered;
                var flag = flags.Get<string>(country);

                sb.AppendLine($"**{(flag == null ? "" : flag + " ")}{country}**");
                sb.AppendLine($"Confirmed cases: {current.Confirmed} ({(confirmedChange >= 0 ? "+" : "")}{confirmedChange})");
                sb.AppendLine($"Deaths: {current.Deaths} ({(deathsChange >= 0 ? "+" : "")}{deathsChange})");
                sb.AppendLine($"Recovered: {current.Recovered} ({(recoveredChange >= 0 ? "+" : "")}{recoveredChange})");
                sb.AppendLine();
            }
            
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

        private class CoronaCountryTimeEntry 
        {
            [JsonProperty("date")]
            public string Date { get; set; }
            
            [JsonProperty("confirmed")]
            public int Confirmed { get; set; }
            
            [JsonProperty("deaths")]
            public int Deaths { get; set; }
            
            [JsonProperty("recovered")]
            public int Recovered { get; set; }
        }
    }
}