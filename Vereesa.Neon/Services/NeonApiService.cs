using System.Net;
using Newtonsoft.Json;
using RestSharp;
using Vereesa.Neon.Data.Models.NeonApi;
using Vereesa.Core;

namespace Vereesa.Neon.Services
{
    public class NeonApiService : IBotService
    {
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
