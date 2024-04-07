using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RestSharp;
using RestSharp.Authenticators;
using Vereesa.Neon.Configuration;
using Vereesa.Neon.Data.Models.BattleNet;
using Vereesa.Core;

namespace Vereesa.Neon.Services
{
    public class BattleNetApiService
    {
        private ILogger<BattleNetApiService> _logger;
        private BattleNetApiSettings _settings;
        private Dictionary<string, (string token, DateTime expiryDateTime)> _regionTokens =
            new Dictionary<string, (string token, DateTime expiryDateTime)>();

        public BattleNetApiService(BattleNetApiSettings settings, ILogger<BattleNetApiService> logger)
        {
            _logger = logger;
            _settings = settings;
        }

        private string GetAuthToken(string region)
        {
            if (_regionTokens.ContainsKey(region) && _regionTokens[region].expiryDateTime > DateTime.Now)
                return _regionTokens[region].token;

            var client = new RestClient($"https://{region}.battle.net");
            client.Authenticator = new HttpBasicAuthenticator(_settings.ClientId, _settings.ClientSecret);
            var request = new RestRequest("oauth/token", Method.POST);
            request.AddParameter("grant_type", "client_credentials");

            var response = client.Execute(request);

            if (!response.IsSuccessful)
            {
                throw new InvalidOperationException("Failed to perform Battle.net authentication.");
            }

            var deserializedResponse = JsonConvert.DeserializeObject<Dictionary<string, string>>(response.Content);
            var tokenDuration = int.Parse(deserializedResponse["expires_in"]);
            var accessToken = deserializedResponse["access_token"];

            _regionTokens[region] = (token: accessToken, expiryDateTime: DateTime.Now.AddSeconds(tokenDuration));

            return _regionTokens[region].token;
        }

        public void GetAuctionPrice(string itemName)
        {
            // var auctionFiles = ExecuteApiRequest<AuctionFileResponse>("eu", "/wow/auction/data/karazhan", Method.GET);
        }

        private T ExecuteApiRequest<T>(string region, string endpoint, Method method)
        {
            var client = new RestClient($"https://{region}.api.blizzard.com");
            var request = new RestRequest(endpoint, method);
            var token = GetAuthToken(region);

            request.AddParameter("access_token", token);
            request.AddParameter("namespace", "profile-eu"); // maybe static-eu or dynamic-eu as well
            request.AddParameter("locale", "en_GB");

            var response = client.Execute(request);

            if (!response.IsSuccessful)
            {
                throw new InvalidOperationException(
                    $"Failed to execute Battle.net API request: {endpoint}: {response.StatusCode} {response.Content}"
                );
            }

            var deserializedResponse = JsonConvert.DeserializeObject<T>(response.Content);

            return deserializedResponse;
        }

        public string GetCharacterThumbnail(string region, string realm, string characterName)
        {
            try
            {
                var response = ExecuteApiRequest<BattleNetMediaResponse>(
                    region,
                    $"/profile/wow/character/{realm.ToLowerInvariant()}/{characterName.ToLowerInvariant()}/character-media",
                    Method.GET
                );
                return response.AvatarUrl;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get character thumbnail.");
                return "https://us.battle.net/forums/static/images/avatars/avatar-default.png";
            }
        }

        public BattleNetCharacterResponse GetCharacterData(string realm, string characterName, string region)
        {
            // var endpoint = $"profile/wow/character///equipment";
            //var endpoint = $"wow/character/{realm}/{characterName}?fields=stats,items&locale=en_GB";
            var endpoint =
                $"/profile/wow/character/{realm.ToLowerInvariant()}/{characterName.ToLowerInvariant()}/equipment";

            try
            {
                return ExecuteApiRequest<BattleNetCharacterResponse>(region, endpoint, Method.GET);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, ex);
                return null;
            }
        }

        public int GetCharacterHeartOfAzerothLevel(BattleNetCharacterResponse character)
        {
            return (int)(
                character?.EquippedItems
                    .FirstOrDefault(i => i.Slot.Name == "Neck" && i.Name == "Heart of Azeroth")
                    ?.AzeriteDetails.Level.Value ?? 0
            );
        }
    }
}
