using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RestSharp;
using Vereesa.Core.Configuration;
using Vereesa.Core.Interfaces;
using Vereesa.Core.Services;

namespace Vereesa.Core.Integrations
{
	[Obsolete]
	public class TwitterClient : IMessageProvider
	{
		private readonly TwitterClientSettings _settings;
		private readonly ILogger<TwitterClient> _logger;

		public TwitterClient(TwitterClientSettings settings, ILogger<TwitterClient> logger)
		{
			_settings = settings;
			_logger = logger;
		}

		public async Task<IEnumerable<IProvidedMessage>> CheckForNewMessagesAsync()
		{
			throw new NotImplementedException();
		}

		private async Task<string> GetTokenAsync()
		{
			var restClient = new RestClient("https://api.twitter.com");
			var request = new RestRequest("/oauth2/token", Method.POST);

			request.AddHeader("Authorization", $"Basic {GetEncodedCredentials()}");
			request.AddParameter("grant_type", "client_credentials");

			try
			{
				var response = await restClient.ExecuteTaskAsync(request);
				var result = JsonConvert.DeserializeObject<dynamic>(response.Content);
				return result["access_token"];
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to retrieve token.");
				throw new AccessViolationException();
			}
		}

		private string GetEncodedCredentials()
		{
			var clientId = _settings.ClientId;
			var clientSecret = _settings.ClientSecret;
			return Convert.ToBase64String(new UTF8Encoding().GetBytes(clientId + ":" + clientSecret));
		}

		public async Task<IList<Tweet>> GetLatestTweetsAsync(string twitterUser)
		{
			var token = await GetTokenAsync();

			var client = new RestClient("https://api.twitter.com");
			var request = new RestRequest("/1.1/statuses/user_timeline.json", Method.GET);
			request.AddQueryParameter("screen_name", twitterUser);
			request.AddQueryParameter("count", "3");
			request.AddQueryParameter("tweet_mode", "extended");
			request.AddQueryParameter("exclude_replies", "true");
			request.AddQueryParameter("include_rts", "false");

			request.AddHeader("Authorization", $"Bearer {token}");

			var response = client.Execute(request);

			var tweets = JsonConvert.DeserializeObject<List<Tweet>>(response.Content);

			return tweets;
		}
	}
}
