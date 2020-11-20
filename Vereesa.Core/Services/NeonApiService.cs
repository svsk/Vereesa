using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Discord.WebSocket;
using Newtonsoft.Json;
using RestSharp;
using Vereesa.Core.Infrastructure;
using Vereesa.Data.Models.NeonApi;

namespace Vereesa.Core.Services
{
    public class NeonApiService : BotServiceBase
    {
		public NeonApiService(DiscordSocketClient discord) 
			: base(discord)
		{
		}

		public async Task<IEnumerable<Application>> GetApplicationsAsync() 
        {
            var restClient = new RestClient("https://api.neon.gg");
            var request = new RestRequest("application", Method.GET);

            var response = await restClient.ExecuteTaskAsync(request);

            if (response.StatusCode == HttpStatusCode.OK) 
            {
                return JsonConvert.DeserializeObject<List<Application>>(response.Content);
            }

            return null;
        }

        public async Task<IEnumerable<ApplicationListItem>> GetApplicationListAsync() 
        {
            var restClient = new RestClient("https://api.neon.gg/");
            var restRequest = new RestRequest("/application/list");
            var result = await restClient.ExecuteTaskAsync<List<ApplicationListItem>>(restRequest);
            return result.Data;
        }

        public async Task<Application> GetApplicationByIdAsync(string applicationId) 
        {
            var restClient = new RestClient("https://api.neon.gg/");
            var restRequest = new RestRequest($"/application/{applicationId}");
            var result = await restClient.ExecuteTaskAsync<Application>(restRequest);
            return result.Data;
        }
    }
}