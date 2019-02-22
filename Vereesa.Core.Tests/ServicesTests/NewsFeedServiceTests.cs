using System.Net;
using System.Threading;
using Discord;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Vereesa.Core.Configuration;
using Vereesa.Core.Integrations;
using Vereesa.Core.Integrations.Interfaces;
using Vereesa.Core.Services;
using Vereesa.Core.Tests.Mocks;
using Vereesa.Data.Models.NewsFeed;

namespace Vereesa.Core.Tests.ServicesTests
{
    [TestClass]
    public class NewsFeedServiceTests
    {
        private NewsFeedServiceSettings _settings;
        private DiscordClientMock _discordMock;
        private IWebClientWrapper _webClient;
        private string _newsListPage = "<div class=\"wrapper-div\"><div>Other div</div><a href=\"/test\"><div id=\"target-div\" class=\"test-class\">Value to check</div></a></div>";
        private string _newsItemPage = $"<div><img src=\"/test/img/fresh.jpg\" /><h1>Fresh Prince</h1><div class=\"content\">This is a story all about how my life got flipped right upside down.</div></div>";

        [TestInitialize]
        public void TestInitialize() 
        {
            _settings = new NewsFeedServiceSettings 
            {
                ChannelId = 1,
                SiteRoot = "https://www.vg.no",
                CheckUrl = "https://www.vg.no",
                CheckInterval = 200,
                CheckElementSelector = "#target-div",
                ReactionLinkSelector = "a",
                NewsItemHeaderSelector = "h1",
                NewsItemImageSelector = "img",
                NewsItemTextSelector = ".content"
            };

            _discordMock = new DiscordClientMock();

            var webClient = new MockWebClientWrapper();
            webClient.UpsertUrlContent("https://www.vg.no", _newsListPage);
            webClient.UpsertUrlContent("https://www.vg.no/test", _newsItemPage);
            _webClient = webClient;
        }

        // [TestInitialize]
        // public void TestInitialize() 
        // {
        //     _settings = new NewsFeedServiceSettings 
        //     {
        //         ChannelId = 1,
        //         SiteRoot = "https://pokemongolive.com",
        //         CheckUrl = "https://pokemongolive.com/en/post/",
        //         CheckInterval = 30000,
        //         CheckElementSelector = ".post-list a",
        //         ReactionLinkSelector = ".post-list a",
        //         NewsItemHeaderSelector = "h1.post__title",
        //         NewsItemImageSelector = ".image img",
        //         NewsItemTextSelector = ".grid__item--10-cols--gt-md"
        //     };

        //     _discordMock = new DiscordClientMock();

        //     _webClient = new WebClientWrapper();
        // }


        [TestMethod]
        public void CreateClass_ObjectConstructing_ObjectConstructedOK() 
        {
            //Arrange

            //Act
            var target = new NewsFeedService(new DiscordClientMock(), _settings, _webClient);

            //Assert
            Assert.IsNotNull(target);
        }

        [TestMethod]
        public void RaiseChangeEvent_HandlingChangeEvent_ChangeEventHandled() 
        {
            //Arrange
            var target = new NewsFeedService(_discordMock, _settings, _webClient);

            //Act
            ((MockWebClientWrapper)_webClient).UpsertUrlContent("https://www.vg.no", _newsListPage.Replace("Value to check", "Changed value"));
            Thread.Sleep(_settings.CheckInterval + 100);
            var channelMock = _discordMock.GetChannelAsync(1).GetAwaiter().GetResult();
            var message = ((IMessageChannel)channelMock).GetMessageAsync(1).GetAwaiter().GetResult();

            //Assert
            Assert.IsNotNull(message);
            Assert.AreEqual(1, message.Embeds.Count);
        }

        [TestMethod]
        public void SelectElementText_SelectingCssElement_ElementContentReturned() 
        {
            //Arrange
            var targetContent = "Target content";
            var target = new NewsFeedService(_discordMock, _settings, _webClient);
            var html = $"<div>Test<ul><li>Nest</li><li>Bar</li></ul><footer>{targetContent}</footer></div>";

            //Act
            var selectedContent = target.SelectElementText(html, "footer");

            //Assert
            Assert.AreEqual(targetContent, selectedContent);
        }

        [TestMethod]
        public void GetNewContent_ExtractingNewContent_NewContentReturned() 
        {
            //Arrange
            var target = new NewsFeedService(_discordMock, _settings, _webClient);

            //Act
            var content = target.GetNewsItem();

            //Assert
            Assert.IsNotNull(content);
            Assert.AreEqual("Fresh Prince", content.Header);
            Assert.AreEqual("https://www.vg.no/test/img/fresh.jpg", content.ImageUrl);
            Assert.AreEqual("This is a story all about how my life got flipped right upside down.", content.Text);
            Assert.AreEqual("https://www.vg.no/test", content.LinkUrl);
        }

        [TestMethod]
        public void BuildDiscordEmbed_BuildingFromNewsItem_DiscordEmbedReturned() 
        {
            //Arrange
            var target = new NewsFeedService(_discordMock, _settings, _webClient);
            var newsItem = new NewsItem 
            {
                Header = "Test header",
                LinkUrl = "https://www.neon.gg/",
                Text = "Neon is a guild on Karazhan EU. We are brand new, and looking for members to clear the first wing in ICC. We only do 10 man raiding and stuff.",
                ImageUrl = "https://www.neon.gg/exampleimage.png"
            };

            //Act
            var embed = target.BuildDiscordEmbed(newsItem);

            //Assert
            Assert.IsNotNull(embed);
            Assert.AreEqual(newsItem.Header, embed.Title);
            Assert.AreEqual(newsItem.ImageUrl, embed.Image.ToString());
            Assert.AreEqual(newsItem.LinkUrl, embed.Url);
            Assert.AreEqual(newsItem.Text, embed.Description);
        }
    }
}