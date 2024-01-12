using System.Net;
using System.Threading;
using Discord;
using Discord.WebSocket;
using Xunit;
using Moq;
using Vereesa.Neon.Configuration;
using Vereesa.Neon.Services;
using Vereesa.Neon.Data.Models.NewsFeed;

namespace Vereesa.Core.Tests.ServicesTests
{
    public class NewsFeedServiceTests
    {
        private NewsFeedServiceSettings _settings;
        private DiscordSocketClient _discordMock;
        private WebClient _webClient;
        private string _newsListPage =
            "<div class=\"wrapper-div\"><div>Other div</div><a href=\"/test\"><div id=\"target-div\" class=\"test-class\">Value to check</div></a></div>";
        private string _newsItemPage =
            $"<div><img src=\"/test/img/fresh.jpg\" /><h1>Fresh Prince</h1><div class=\"content\">This is a story all about how my life got flipped right upside down.</div></div>";

        public NewsFeedServiceTests()
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

            _discordMock = new Mock<DiscordSocketClient>().Object;

            var webClient = new Mock<WebClient>().Object;

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


        [Fact]
        public void CreateClass_ObjectConstructing_ObjectConstructedOK()
        {
            //Arrange

            //Act
            var target = new NewsFeedService(_discordMock, _settings, _webClient);

            //Assert
            Assert.NotNull(target);
        }

        [Fact]
        public void RaiseChangeEvent_HandlingChangeEvent_ChangeEventHandled()
        {
            //Arrange
            var target = new NewsFeedService(_discordMock, _settings, _webClient);

            //Act
            // ((MockWebClientWrapper)_webClient.UpsertUrlContent("https://www.vg.no", _newsListPage.Replace("Value to check", "Changed value"));
            Thread.Sleep(_settings.CheckInterval + 100);
            var channelMock = _discordMock.GetChannel(1);
            var message = ((IMessageChannel)channelMock).GetMessageAsync(1).GetAwaiter().GetResult();

            //Assert
            Assert.NotNull(message);
            Assert.Equal(1, message.Embeds.Count);
        }

        [Fact]
        public void SelectElementText_SelectingCssElement_ElementContentReturned()
        {
            //Arrange
            var targetContent = "Target content";
            var target = new NewsFeedService(_discordMock, _settings, _webClient);
            var html = $"<div>Test<ul><li>Nest</li><li>Bar</li></ul><footer>{targetContent}</footer></div>";

            //Act
            var selectedContent = target.SelectElementText(html, "footer");

            //Assert
            Assert.Equal(targetContent, selectedContent);
        }

        [Fact]
        public void GetNewContent_ExtractingNewContent_NewContentReturned()
        {
            //Arrange
            var target = new NewsFeedService(_discordMock, _settings, _webClient);

            //Act
            var content = target.GetNewsItem();

            //Assert
            Assert.NotNull(content);
            Assert.Equal("Fresh Prince", content.Header);
            Assert.Equal("https://www.vg.no/test/img/fresh.jpg", content.ImageUrl);
            Assert.Equal("This is a story all about how my life got flipped right upside down.", content.Text);
            Assert.Equal("https://www.vg.no/test", content.LinkUrl);
        }

        [Fact]
        public void BuildDiscordEmbed_BuildingFromNewsItem_DiscordEmbedReturned()
        {
            //Arrange
            var target = new NewsFeedService(_discordMock, _settings, _webClient);
            var newsItem = new NewsItem
            {
                Header = "Test header",
                LinkUrl = "https://www.neon.gg/",
                Text =
                    "Neon is a guild on Karazhan EU. We are brand new, and looking for members to clear the first wing in ICC. We only do 10 man raiding and stuff.",
                ImageUrl = "https://www.neon.gg/exampleimage.png"
            };

            //Act
            var embed = target.BuildDiscordEmbed(newsItem);

            //Assert
            Assert.NotNull(embed);
            Assert.Equal(newsItem.Header, embed.Title);
            Assert.Equal(newsItem.ImageUrl, embed.Image.ToString());
            Assert.Equal(newsItem.LinkUrl, embed.Url);
            Assert.Equal(newsItem.Text, embed.Description);
        }
    }
}
