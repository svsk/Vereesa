using System.Net;
using Newtonsoft.Json;
using RestSharp;
using Vereesa.Core;
using Vereesa.Neon.Data.Models.NeonApi;

namespace Vereesa.Neon.Services
{
    public class NeonApiService : IBotModule
    {
        public async Task<IEnumerable<Application>?> GetApplicationsAsync()
        {
            var restClient = new RestClient("https://api.neon.gg");
            var request = new RestRequest("application", Method.Get);

            var response = await restClient.ExecuteAsync(request);

            if (response.StatusCode == HttpStatusCode.OK && response.Content != null)
            {
                return JsonConvert.DeserializeObject<List<Application>>(response.Content);
            }

            return null;
        }

        public async Task<IEnumerable<ApplicationListItem>?> GetApplicationListAsync()
        {
            var restClient = new RestClient("https://api.neon.gg/");
            var restRequest = new RestRequest("/application/list");
            var response = await restClient.ExecuteAsync(restRequest);

            if (response.StatusCode == HttpStatusCode.OK && response.Content != null)
            {
                return JsonConvert.DeserializeObject<List<ApplicationListItem>>(response.Content);
            }

            return null;
        }

        public async Task<Application?> GetApplicationByIdAsync(string applicationId)
        {
            var restClient = new RestClient("https://api.neon.gg/");
            var restRequest = new RestRequest($"/application/{applicationId}");
            var result = await restClient.ExecuteAsync(restRequest);

            if (result.StatusCode == HttpStatusCode.OK && result.Content != null)
            {
                return JsonConvert.DeserializeObject<Application>(result.Content);
            }

            return null;
        }
    }
}
