using System.ComponentModel;
using Discord;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NodaTime;
using RestSharp;
using Vereesa.Core.Extensions;
using Vereesa.Neon.Extensions;
using Vereesa.Core.Infrastructure;

namespace Vereesa.Neon.Services
{
    public class AuctionHouseService : IBotService
    {
        private ILogger<AuctionHouseService> _logger;
        private RestClient _restClient;

        public AuctionHouseService(ILogger<AuctionHouseService> logger)
        {
            _logger = logger;
            _restClient = new RestClient("https://theunderminejournal.com/api/");
        }

        [OnCommand("!ah")]
        [WithArgument("itemName", 0)]
        [Description("Checks price of an item on the Auction House. Uses the Undermine Journal as backing data.")]
        [AsyncHandler]
        public async Task HandleMessageReceivedAsync(IMessage message, string itemName)
        {
            Embed embed = null;
            var itemStats = GetPriceInformation(itemName);

            if (itemStats.item != null)
            {
                var item = itemStats.item.Stats.First();

                // Build the embed
                var builder = new EmbedBuilder();
                builder.WithTitle(item.Name);
                builder.WithThumbnailUrl(
                    $"https://theunderminejournal.com/icon/large/{item.Icon.Replace(" ", "")}.jpg"
                );
                builder.Color = item.QualityColor;

                builder.AddField("__Current Price__", $"{item.GoldPrice}g", true);
                builder.AddField(
                    "__Current Quantity__",
                    item.Quantity.ToString("#,0", StringExtensions.GetThousandSeparatorFormat()),
                    true
                );

                var auctions = itemStats.item.Auctions.AuctionList;
                var topAuctions = auctions
                    .OrderBy(auc => auc.BuyoutPrice / auc.Quantity)
                    .Take(5)
                    .Select(auc =>
                    {
                        return $"{auc.Quantity} @ {((auc.BuyoutPrice / auc.Quantity) / 10000).ToString("#,0.00", StringExtensions.GetThousandSeparatorFormat())}g each";
                    });

                builder.AddField("__Top Auctions__", string.Join("\r\n", topAuctions), false);

                var externalSites = new string[]
                {
                    $"[Undermine Journal](https://theunderminejournal.com/#eu/karazhan/item/{item.Id})",
                    $"[Wowhead](https://www.wowhead.com/item=168487/{item.Id})",
                    $"[Wowuction.com](https://www.wowuction.com/eu/karazhan/Items/Stats/{item.Id})"
                };

                builder.AddField("__External sites__", string.Join(" | ", externalSites), false);

                builder.Footer = new EmbedFooterBuilder();

                var lastRefreshTime = Instant.FromDateTimeUtc(item.LastSeen).AsServerTime().ToPrettyTime();

                var requestTime = DateTimeExtensions.NowInCentralEuropeanTime().ToString("HH:mm");

                var footerText = new string[]
                {
                    $"Requested by {message.Author.Username} today at {requestTime}",
                    $"Auction data last refreshed at {lastRefreshTime}"
                };

                builder.Footer.Text = string.Join(" | ", footerText);

                embed = builder.Build();
            }

            await message.Channel.SendMessageAsync(itemStats.message, embed: embed);
        }

        private (string message, UndermineResult item) GetPriceInformation(string itemName)
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
            var request = new RestRequest("/item.php", Method.GET);
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
            var request = new RestRequest("/search.php", Method.GET);
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

        private class UndermineItem
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
                        case 0: // Poor
                            return new Color(157, 157, 157);
                        default:
                        case 1: // Common
                            return new Color(254, 254, 254);
                        case 2: // Uncommon
                            return new Color(30, 254, 0);
                        case 3: // Rare
                            return new Color(0, 112, 221);
                        case 4: // Epic
                            return new Color(163, 53, 238);
                        case 5: // Legendary
                            return new Color(254, 128, 0);
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

        private class UndermineResult
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

        private class UndermineAuction
        {
            [JsonProperty("id")]
            public long Id { get; set; }

            [JsonProperty("quantity")]
            public long Quantity { get; set; }

            [JsonProperty("buy")]
            public decimal BuyoutPrice { get; set; }
        }

        private class UndermineItemSearchResult
        {
            [JsonProperty("id")]
            public long Id { get; set; }

            [JsonProperty("name_enus")]
            public string Name { get; set; }
        }
    }
}
