using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RestSharp;
using Vereesa.Data.Models.NeonApi;

namespace Vereesa.Core.Services
{
    public class NeonApiService
    {
        public NeonApiService()
        {
        }

        public IEnumerable<Application> GetApplications() 
        {
            var restClient = new RestClient("https://api.neon.gg");
            var request = new RestRequest("application", Method.GET);

            var response = restClient.Execute(request);

            if (response.StatusCode == HttpStatusCode.OK) 
            {
                return JsonConvert.DeserializeObject<List<Application>>(response.Content);
            }

            return null;
        }
    }
}