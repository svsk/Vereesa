using Discord;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RestSharp;
using Vereesa.Core.Extensions;
using Vereesa.Neon.Helpers;

namespace Vereesa.Neon.Services
{
    public class AuctionHouseService
    {
        private ILogger<AuctionHouseService> _logger;
        private RestClient _restClient;

        public AuctionHouseService(ILogger<AuctionHouseService> logger)
        {
            _logger = logger;
            _restClient = new RestClient("https://theunderminejournal.com/api/");
        }

        public (string message, UndermineResult item) GetPriceInformation(string itemName)
        {
            try
            {
                var searchResult = GetUndermineItem(itemName);
                UndermineResult itemDetails = GetUndermineItemDetails(searchResult.Id);

                return (string.Empty, itemDetails);
            }
            catch (CouldNotFindItemException)
            {
                return ("I wasn't able to find the item you asked about.", null);
            }
            catch (CouldNotGetPriceException)
            {
                return ("I wasn't able to find the price for that item.", null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to lookup price.");
                return ("I was unable to complete your request.", null);
            }
        }

        private UndermineResult GetUndermineItemDetails(long itemId)
        {
            var request = new RestRequest("/item.php", Method.Get);
            request.AddQueryParameter("house", "177"); // Karazhan AH ID
            request.AddQueryParameter("item", itemId.ToString());

            var response = _restClient.Execute(request);
            var result = JsonConvert.DeserializeObject<UndermineResult>(
                response.Content,
                new JsonSerializerSettings { DateTimeZoneHandling = DateTimeZoneHandling.Utc }
            );

            if (response.IsSuccessful && result.Stats?.Count > 0)
            {
                return result;
            }
            else
            {
                throw new CouldNotGetPriceException();
            }
        }

        private UndermineItemSearchResult GetUndermineItem(string itemName)
        {
            var request = new RestRequest("/search.php", Method.Get);
            request.AddQueryParameter("locale", "enus");
            request.AddQueryParameter("house", "177"); // Karazhan AH ID
            request.AddQueryParameter("search", itemName, true);

            var response = _restClient.Execute<UndermineResult>(request);

            if (response.IsSuccessful && response.Data.Items?.Count > 0)
            {
                return response.Data.Items.FirstOrDefault();
            }
            else
            {
                throw new CouldNotFindItemException();
            }
        }

        public class UndermineItem
        {
            [JsonProperty("id")]
            public long Id { get; set; }

            [JsonProperty("name_enus")]
            public string Name { get; set; }

            [JsonProperty("quantity")]
            public long Quantity { get; set; }

            [JsonProperty("price")]
            public decimal Price { get; set; }

            [JsonProperty("lastseen")]
            public DateTime LastSeen { get; set; }

            [JsonProperty("icon")]
            public string Icon { get; set; }

            [JsonProperty("quality")]
            public int Quality { get; set; }

            public Color QualityColor
            {
                get
                {
                    switch (Quality)
                    {
                        case 0:
                            return VereesaColors.Poor;
                        default:
                        case 1:
                            return VereesaColors.Common;
                        case 2:
                            return VereesaColors.Uncommon;
                        case 3:
                            return VereesaColors.Rare;
                        case 4:
                            return VereesaColors.Epic;
                        case 5:
                            return VereesaColors.Legendary;
                    }
                }
            }

            public string GoldPrice
            {
                get { return (Price / 10000).ToString("#,0.00", StringExtensions.GetThousandSeparatorFormat()); }
            }
        }

        private class CouldNotFindItemException : Exception { }

        private class CouldNotGetPriceException : Exception { }

        public class UndermineResult
        {
            [JsonProperty("items")]
            public List<UndermineItemSearchResult> Items { get; set; }

            [JsonProperty("stats")]
            public List<UndermineItem> Stats { get; set; }

            [JsonProperty("auctions")]
            public UndermineResult Auctions { get; set; }

            [JsonProperty("data")]
            public List<UndermineAuction> AuctionList { get; set; }
        }

        public class UndermineAuction
        {
            [JsonProperty("id")]
            public long Id { get; set; }

            [JsonProperty("quantity")]
            public long Quantity { get; set; }

            [JsonProperty("buy")]
            public decimal BuyoutPrice { get; set; }
        }

        public class UndermineItemSearchResult
        {
            [JsonProperty("id")]
            public long Id { get; set; }

            [JsonProperty("name_enus")]
            public string Name { get; set; }
        }
    }
}
