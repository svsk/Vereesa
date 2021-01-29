using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RestSharp;
using Vereesa.Core.Configuration;

namespace Vereesa.Core.Integrations
{
	public interface IWarcraftLogsApi
	{
		Task<List<Report>> GetRaidReports();

		Task<List<ReportCharacter>> GetRaidComposition(string id, long windowStart, long windowEnd);
	}

	public class WarcraftLogsApi : IWarcraftLogsApi
	{
		private string _apiKey;

		public WarcraftLogsApi(WarcraftLogsApiSettings settings)
		{
			_apiKey = settings?.ApiKey;
		}

		public async Task<List<Report>> GetRaidReports()
		{
			var restClient = new RestClient();
			var request = new RestRequest("https://www.warcraftlogs.com:443/v1/reports/guild/Neon/Karazhan/EU", Method.GET);
			request.AddQueryParameter("api_key", _apiKey);

			var response = await restClient.ExecuteAsync<List<Report>>(request);

			return response.Data;
		}

		public async Task<List<ReportCharacter>> GetRaidComposition(string id, long windowStart, long windowEnd)
		{
			var restClient = new RestClient();
			var request = new RestRequest($"https://www.warcraftlogs.com:443/v1/report/tables/summary/{id}/", Method.GET);
			request.AddQueryParameter("api_key", _apiKey);
			request.AddQueryParameter("start", windowStart.ToString());
			request.AddQueryParameter("end", windowEnd.ToString());

			var response = await restClient.ExecuteAsync<ReportySummary>(request);

			return response.Data.Composition;
		}
	}


	public class ReportySummary
	{
		[JsonProperty("composition")]
		public List<ReportCharacter> Composition { get; set; }
	}

	public class ReportCharacter
	{
		[JsonProperty("name")]
		public string Name { get; set; }

		[JsonProperty("id")]
		public int Id { get; set; }

		[JsonProperty("guid")]
		public long Guid { get; set; }

		[JsonProperty("type")]
		public string Type { get; set; }

		[JsonProperty("specs")]
		public List<Specialization> Specs { get; set; }
	}

	public class Specialization
	{
		[JsonProperty("spec")]
		public string Spec { get; set; }

		[JsonProperty("role")]
		public string Role { get; set; }
	}

	public class Report
	{
		[JsonProperty("id")]
		public string Id { get; set; }

		[JsonProperty("title")]
		public string Title { get; set; }

		[JsonProperty("owner")]
		public string Owner { get; set; }

		[JsonProperty("start")]
		public long Start { get; set; }

		[JsonProperty("end")]
		public long End { get; set; }

		[JsonProperty("zone")]
		public int Zone { get; set; }

	}
}