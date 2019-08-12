using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using Discord;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;
using Vereesa.Core.Configuration;
using Vereesa.Core.Helpers;
using Vereesa.Core.Integrations.Interfaces;
using Vereesa.Data.Models.NewsFeed;

namespace Vereesa.Core.Services
{
    public class NewsFeedService
    {
        private IDiscordSocketClient _discord;
        private Timer _interval;
        private Func<object, Task> _changeHappened;
        private NewsFeedServiceSettings _settings;
        private string _previousState;
        private IWebClientWrapper _webClient;
        

        public NewsFeedService(IDiscordSocketClient discord, NewsFeedServiceSettings settings, IWebClientWrapper webClient)
        {
            _discord = discord;
            _settings = settings;
            _webClient = webClient;
            _changeHappened += HandleChangeHappenedAsync;
        }

        public async Task InitializeServiceAsync() 
        {
            await StartCheckingForChangesAsync();
        }

        private async Task StartCheckingForChangesAsync()
        {
            _interval = await TimerHelpers.SetTimeoutAsync(CheckForChangeAsync, _settings.CheckInterval, true, true);
        }

        private async Task CheckForChangeAsync()
        {
            if (TargetHasChanged())
            {
                await _changeHappened?.Invoke(this);
            }
        }

        private bool TargetHasChanged()
        {
            string currentState = string.Empty;
            string rawHtml = _webClient.DownloadString(_settings.CheckUrl);

            currentState = SelectElementText(rawHtml, _settings.CheckElementSelector);

            bool hasChanged = (string.IsNullOrEmpty(_previousState) || currentState == _previousState);
            _previousState = currentState;

            return hasChanged;
        }

        private async Task HandleChangeHappenedAsync(object sender)
        {
            var newsItem = GetNewsItem();
            var embed = BuildDiscordEmbed(newsItem);
            await SendNewsItemEmbedAsync(embed);
        }

        private HtmlNode SelectElement(string html, string selector)
        {
            var doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(html);

            var elements = doc.QuerySelectorAll(selector);

            if (elements.Count > 1)
            {
                //throw new InvalidDataException("Selector returned more than one element.");
                //Log this as a warning maybe?
            }

            if (elements.Count == 0)
            {
                throw new InvalidDataException("Selector returned no elements.");
            }

            return elements.First();
        }

        public string SelectElementText(string html, string selector)
        {
            var element = SelectElement(html, selector);
            return element.InnerText;
        }

        public string SelectElementAttribute(string html, string selector, string attribute)
        {
            var element = SelectElement(html, selector);
            return element.Attributes[attribute].Value;
        }


        public NewsItem GetNewsItem()
        {
            var newsListHtml = _webClient.DownloadString(_settings.CheckUrl);
            var link = SelectElementAttribute(newsListHtml, _settings.ReactionLinkSelector, "href");
            
            if (link.StartsWith("/"))
                link = _settings.SiteRoot + link;

            string newsItemHtml = _webClient.DownloadString(link);

            string header = SelectElementText(newsItemHtml, _settings.NewsItemHeaderSelector);
            string imageUrl = SelectElementAttribute(newsItemHtml, _settings.NewsItemImageSelector, "src");
            string text = SelectElementText(newsItemHtml, _settings.NewsItemTextSelector);

            if (imageUrl.StartsWith("/"))
                imageUrl = _settings.SiteRoot + imageUrl;

            return new NewsItem 
            {
                Header = header,
                ImageUrl = imageUrl,
                Text = text,
                LinkUrl = link
            };
        }

        public Embed BuildDiscordEmbed(NewsItem newsItem)
        {
            var embedBuilder = new EmbedBuilder();
            embedBuilder.ImageUrl = newsItem.ImageUrl;
            embedBuilder.Title = newsItem.Header;
            embedBuilder.Url = newsItem.LinkUrl;
            embedBuilder.Description = newsItem.Text;

            return embedBuilder.Build();
        }

        public async Task SendNewsItemEmbedAsync(Embed embed) 
        {
            var channel = await _discord.GetChannelAsync(_settings.ChannelId);
            await ((IMessageChannel)channel).SendMessageAsync(text: string.Empty, embed: embed);
        }
    }
}