using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using RestSharp;
using RestSharp.Authenticators;
using Vereesa.Core.Configuration;
using Vereesa.Data.Models.BattleNet;
using Vereesa.Data.Models.EventHub;

namespace Vereesa.Core.Services
{
    public class BattleNetApiService
    {
        private ILogger<BattleNetApiService> _logger;
        private BattleNetApiSettings _settings;
        private Dictionary<string, (string token, DateTime expiryDateTime)> _regionTokens = new Dictionary<string, (string token, DateTime expiryDateTime)>();

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

        private T ExecuteApiRequest<T>(string region, string endpoint, Method method)
        {
            var client = new RestClient($"https://{region}.api.blizzard.com");
            var request = new RestRequest(endpoint, method);
            var token = GetAuthToken(region);

            request.AddHeader("Authorization", $"Bearer {token}");

            var response = client.Execute(request);

            if (!response.IsSuccessful) 
            {
                throw new InvalidOperationException($"Failed to execute Battle.net API request: {endpoint}.");
            }

            var deserializedResponse =JsonConvert.DeserializeObject<T>(response.Content);

            return deserializedResponse;
        }

        public string GetCharacterThumbnail(WowCharacter character, string region)
        {
            if (character == null)
                return "https://us.battle.net/forums/static/images/avatars/avatar-default.png";

            return $"https://render-{region}.worldofwarcraft.com/character/{character.Thumbnail}";
        }

        public WowCharacter GetCharacterData(string realm, string characterName, string region)
        {
            var endpoint = $"wow/character/{realm}/{characterName}?fields=stats,items&locale=en_GB";

            try
            {
                return ExecuteApiRequest<WowCharacter>(region, endpoint, Method.GET);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, ex);
                return null;
            }
        }

        public int GetCharacterArtifactTraitCount(WowCharacter character)
        {
            var totalTraits = 0;

            //Get traits from offhand if that's where they are. Otherwise use mainhand.    
            var artifactItem = character.Items.OffHand != null && character.Items.OffHand.ArtifactTraits.Count > 0 ?
                character.Items.OffHand :
                character.Items.MainHand;

            var traits = artifactItem.ArtifactTraits;

            if (traits.Count >= 22 && traits.Last().Rank > 0)
            {
                //Character has Concordance. Concordance requires 51 traits to unlock. So do 51 + number of ranks on Concordance.
                totalTraits = 51 + traits.Last().Rank;
            }
            else
            {
                //Character does not have Concordance, so it's kind of pointless, but let's do it anyway...
                totalTraits = traits.Sum(t => t.Rank) - artifactItem.Relics.Count;
            }

            return totalTraits;
        }

        public int GetCharacterHeartOfAzerothLevel(WowCharacter character) 
        {
            return character.Items?.Neck?.AzeriteItem?.AzeriteLevel ?? 0;
        }
    }
}