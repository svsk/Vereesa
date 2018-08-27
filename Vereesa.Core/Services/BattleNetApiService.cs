using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Vereesa.Core.Configuration;
using Vereesa.Data.Models.BattleNet;
using Vereesa.Data.Models.EventHub;

namespace Vereesa.Core.Services
{
    public class BattleNetApiService
    {
        private BattleNetApiSettings _settings;

        public BattleNetApiService(BattleNetApiSettings settings)
        {
            _settings = settings;
        }

        private async Task<T> GetRequestAsync<T>(string url)
        {
            using (var client = new HttpClient())
            {
                var httpResponse = await client.GetAsync(url);
                var responseText = await httpResponse.Content.ReadAsStringAsync();
                var typedResponse = JsonConvert.DeserializeObject<T>(responseText);
                return typedResponse;
            }
        }

        public string GetCharacterThumbnail(WowCharacter character, string region)
        {
            if (character == null)
                return "https://us.battle.net/forums/static/images/avatars/avatar-default.png";

            return $"https://render-{region}.worldofwarcraft.com/character/{character.Thumbnail}";
        }

        public async Task<WowCharacter> GetCharacterData(string realm, string characterName, string region)
        {
            var url = $"https://{region}.api.battle.net/wow/character/{realm}/{characterName}?fields=stats,items&locale=en_GB&apikey={_settings.ApiKey}";

            try
            {
                return await GetRequestAsync<WowCharacter>(url);
            }
            catch
            {
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