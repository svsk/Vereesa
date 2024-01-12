using Newtonsoft.Json;
using RestSharp;
using Vereesa.Neon.Configuration;

namespace Vereesa.Neon.Integrations
{
    public interface IWarcraftLogsApi
    {
        Task<List<Report>> GetRaidReports();

        Task<List<ReportCharacter>> GetRaidComposition(string id, long windowStart, long windowEnd);
    }

    public class WarcraftLogsApi : IWarcraftLogsApi
    {
        private string? _apiKey;

        private string GetApiKey() => _apiKey != null ? _apiKey : throw new Exception("API key not set");

        public WarcraftLogsApi(WarcraftLogsApiSettings settings)
        {
            _apiKey = settings?.ApiKey;
        }

        public async Task<List<Report>> GetRaidReports()
        {
            var restClient = new RestClient();
            var request = new RestRequest(
                "https://www.warcraftlogs.com:443/v1/reports/guild/Neon/Karazhan/EU",
                Method.GET
            );
            request.AddQueryParameter("api_key", GetApiKey());

            var response = await restClient.ExecuteAsync<List<Report>>(request);

            return response.Data;
        }

        public async Task<List<ReportCharacter>> GetRaidComposition(string id, long windowStart, long windowEnd)
        {
            var restClient = new RestClient();
            var request = new RestRequest(
                $"https://www.warcraftlogs.com:443/v1/report/tables/summary/{id}/",
                Method.GET
            );
            request.AddQueryParameter("api_key", GetApiKey());
            request.AddQueryParameter("start", windowStart.ToString());
            request.AddQueryParameter("end", windowEnd.ToString());

            var response = await restClient.ExecuteAsync<ReportySummary>(request);
            var comp = response.Data.Composition;

            if (comp == null)
            {
                throw new Exception("Composition not found.");
            }

            return comp;
        }
    }

    public class ReportySummary
    {
        [JsonProperty("composition")]
        public List<ReportCharacter>? Composition { get; set; }
    }

    public class ReportCharacter
    {
        [JsonProperty("name")]
        public string? Name { get; set; }

        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("guid")]
        public long Guid { get; set; }

        [JsonProperty("type")]
        public string? Type { get; set; }

        [JsonProperty("specs")]
        public List<Specialization>? Specs { get; set; }
    }

    public class Specialization
    {
        [JsonProperty("spec")]
        public string? Spec { get; set; }

        [JsonProperty("role")]
        public string? Role { get; set; }
    }

    public class Report
    {
        [JsonProperty("id")]
        public string? Id { get; set; }

        [JsonProperty("title")]
        public string? Title { get; set; }

        [JsonProperty("owner")]
        public string? Owner { get; set; }

        [JsonProperty("start")]
        public long Start { get; set; }

        [JsonProperty("end")]
        public long End { get; set; }

        [JsonProperty("zone")]
        public int Zone { get; set; }
    }
}
