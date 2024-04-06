using System.ComponentModel;
using Discord;
using Microsoft.Extensions.Logging;
using NodaTime;
using Vereesa.Core.Extensions;
using Vereesa.Neon.Extensions;
using Vereesa.Core.Infrastructure;
using Vereesa.Core;
using Vereesa.Neon.Services;

namespace Vereesa.Neon.Modules
{
    public class AuctionHouseModule : IBotModule
    {
        private readonly AuctionHouseService _service;

        public AuctionHouseModule(AuctionHouseService service, ILogger<AuctionHouseModule> logger)
        {
            _service = service;
        }

        [OnCommand("!ah")]
        [WithArgument("itemName", 0)]
        [Description("Checks price of an item on the Auction House. Uses the Undermine Journal as backing data.")]
        [AsyncHandler]
        public async Task HandleMessageReceivedAsync(IMessage message, string itemName)
        {
            Embed embed = null;
            var itemStats = _service.GetPriceInformation(itemName);

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
                        return $"{auc.Quantity} @ {(auc.BuyoutPrice / auc.Quantity / 10000).ToString("#,0.00", StringExtensions.GetThousandSeparatorFormat())}g each";
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
    }
}
