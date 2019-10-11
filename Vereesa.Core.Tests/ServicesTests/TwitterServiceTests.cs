using Microsoft.VisualStudio.TestTools.UnitTesting;
using Vereesa.Core.Configuration;
using Vereesa.Core.Services;
using Vereesa.Core.Tests.Mocks;

namespace Vereesa.Core.Tests.ServicesTests
{
    [TestClass]
    public class TwitterServiceTests
    {
        // [TestMethod]
        // public void GetTweets_GettingTweetsByUser_ReturnedTweets() 
        // {
        //     //Arrange
        //     var settings = new TwitterSettings();
        //     settings.ClientId = "iSl8ZxpHOVIKGRE68SrxZGvkG";
        //     settings.ClientSecret = "kgokMkcaC68ugUrlgfQzmUD6arPjwJ0rc3O0RRLFs2mRL8JKKl";
        //     settings.SourceTwitterUser = "swuden";
        //     settings.TargetDiscordGuild = "Neon";
        //     settings.TargetDiscordChannel = "#pokémon_go";
        //     settings.CheckIntervalSeconds = 1200000;

        //     var discord = new DiscordClientMock();
        //     var target = new TwitterService(settings, discord);

        //     //Act
        //     var tweets = target.GetLatestTweetsAsync().GetAwaiter().GetResult();

        //     //Assert
        //     Assert.IsTrue(tweets[0].IsRetweet);
        //     Assert.IsTrue(tweets[1].IsReply);
        //     Assert.IsFalse(tweets[2].IsReply);
        //     Assert.IsFalse(tweets[2].IsRetweet);
        // }
    }
}