using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RestSharp;
using Vereesa.Core.Extensions;
using Vereesa.Core.Integrations.Interfaces;
using Vereesa.Data.Interfaces;
using Vereesa.Data.Models.Statistics;

namespace Vereesa.Core.Services
{
    public class CoronaService : BotServiceBase
    {
        private DiscordSocketClient _discord;
        private IRepository<Statistics> _statRepository;
        private ILogger<CoronaService> _logger;
        private Statistics _coronaStats => _statRepository.FindById("corona") ?? new Statistics { Id = "corona" };
        private Statistics _flags => _statRepository.FindById("flags") ?? new Statistics { Id = "flags" };

        public CoronaService(DiscordSocketClient discord, IRepository<Statistics> statRepository, ILogger<CoronaService> logger)
			:base(discord)
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
                var messageToUpdate = await message.Channel.SendMessageAsync($"OK, checking...");
                var result = GetCoronaReport(handlers => 
                    handlers.OnStepCompleted = (progress) => messageToUpdate.ModifyAsync(msg => msg.Content = progress.ToString()).GetAwaiter().GetResult()
                );
                await messageToUpdate.ModifyAsync(msg => msg.Content = result);
            }

            if (command == "!setneoninfected") 
            {
                await message.Channel.SendMessageAsync(SetGuildInfected(message.GetCommandArgs()));
            }

            if (command == "!setneonrecovered") 
            {
                await message.Channel.SendMessageAsync(SetGuildRecovered(message.GetCommandArgs()));
            }

            if (command == "!addcoronacountry") 
            {
                await message.Channel.SendMessageAsync(AddRelevantCountry(message.GetCommandArgs()));
            }

            if (command == "!removecoronacountry") 
            {
                await message.Channel.SendMessageAsync(RemoveRelevantCountry(message.GetCommandArgs()));
            }

            if (command == "!checkcoronacountry") 
            {
                await message.Channel.SendMessageAsync(CheckCoronaCountry(message.GetCommandArgs()));
            }
        }

        private string CheckCoronaCountry(string[] countryWords)
        {
            if (countryWords == null || !countryWords.Any()) 
            {
                return "Please specify a country";
            }

            var country = string.Join(" ", countryWords);
            var countryStats = GetCountryStats(country, out var error);
            if (countryStats == null) 
            {
                return "Couldn't find that country in my stats. Please ensure that you spelt it correctly and that it has Title Casing.";
            }

            var sb = new StringBuilder();
            sb.AppendLine($"Here are the Corona stats for {country}");

            AppendRow(sb, countryStats);

            return sb.ToString();
        }

        private string AddRelevantCountry(string[] countryWords)
        {
            if (countryWords == null || !countryWords.Any()) 
            {
                return "Please specify a country";
            }

            var country = string.Join(" ", countryWords);
            var countryStats = GetCountryStats(country, out var error);
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

            UpdateStat(numberOfInfected, "guildInfected");

            return $"There are now {numberOfInfected} registered cases of Neon members infected with Corona virus.";
        }

        private string SetGuildRecovered(string[] commandArgs) 
        {
            if (!int.TryParse(commandArgs.FirstOrDefault(), out var numberOfRecovered)) 
            {
                return "I don't get what you mean. You need to add a number to your command.";
            }

            UpdateStat(numberOfRecovered, "guildRecovered");

            return $"There are now {numberOfRecovered} Neon members who have recovered from Corona virus.";
        }

        private string GetCoronaReport(Action<ProgressHandlers> progress)
        {
            var handlers = new ProgressHandlers();
            progress.Invoke(handlers);

            return $"**:rotating_light: Corona virus pandemic is ongoing :rotating_light:** {GenerateRelevantCountryReport(handlers)}";
        }

        private CoronaCountryStats GetCountryStats(string countryName, out string error) 
        {
            error = null;
            var restClient = new RestClient("https://disease.sh/v2/countries/");
            var restRequest = new RestRequest(countryName, Method.GET);

            var response = restClient.Execute(restRequest);

            if (!response.IsSuccessful) 
            {
                error = response.StatusDescription;
                return null;
            }

            try 
            {
                return JsonConvert.DeserializeObject<CoronaCountryStats>(response.Content);
            }
            catch 
            {
                error = "Deserialization error";
                return null;
            }
        }

        private void AppendRow(StringBuilder builder, CoronaCountryStats countryStats) 
        {
            var flag = _flags.Get<string>(countryStats.Name);

            builder.AppendLine($"**{(flag == null ? "" : flag + " ")}{countryStats.Name}**");
            builder.AppendLine($"Confirmed cases: {countryStats.Cases} (+{countryStats.TodayCases})");
            builder.AppendLine($"Deaths: {countryStats.Deaths} (+{countryStats.TodayDeaths})");
            builder.AppendLine($"Recovered: {countryStats.Recovered}");
            builder.AppendLine();
        }

        private string GenerateRelevantCountryReport(ProgressHandlers progressHandlers) 
        {
            var relevantCountries = GetRelevantCountries();
            var flags = _flags;

            var sb = new StringBuilder();
            sb.AppendLine();

            void AppendErrorRow(StringBuilder builder, string country, string errorReason) 
            {
                string flag = flags.Get<string>(country);
                flag = flag == null ? string.Empty : flag + " ";
                builder.AppendLine($"**{flag}{country}**");
                builder.AppendLine($"❌ Failed to get data from the data source: {errorReason}");
                builder.AppendLine();
            };

            var distinctCountries = relevantCountries.Distinct().ToList();
            foreach (var country in distinctCountries.Select((country, index) => new { Name = country, index })) 
            {
                progressHandlers.OnStepCompleted?.Invoke($"`⌛ Checking {country.index+1}/{distinctCountries.Count}: {country.Name}...`");

                var stats = GetCountryStats(country.Name, out var error);
                if (stats != null) 
                {
                    AppendRow(sb, stats);
                } 
                else 
                {
                    AppendErrorRow(sb, country.Name, error);
                }
            }

            var neonInfected = GetStat("guildInfected") ?? 0;
            var neonRecovered = GetStat("guildRecovered") ?? 0;
            AppendRow(sb, new CoronaCountryStats {
                Name = "Neon",
                Cases = neonInfected,
                TodayCases = 0,
                Deaths = 0,
                TodayDeaths = 0,
                Recovered = neonRecovered
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

        private int? GetStat(string targetKey) 
        {
            try 
            {
                return _coronaStats.Get<int>(targetKey);
            } 
            catch 
            {
                return null;
            }
        }

        private void UpdateStat(int numberInStat, string targetKey) 
        {
            var stats = _coronaStats;

            if (stats.Stats.ContainsKey(targetKey)) 
            {
                stats.Stats[targetKey] = numberInStat;
            } 
            else 
            {
                stats.Stats.Add(targetKey, numberInStat);
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

    public class ProgressHandlers
    {
        public Action<object> OnStepCompleted { get; set; }
    }
}