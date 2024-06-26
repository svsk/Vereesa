using System;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Xunit;
using Moq;
using Vereesa.Neon.Configuration;
using Vereesa.Neon.Services;

namespace Vereesa.Neon.Tests.ServicesTests
{
    public class BattleNetApiServiceTests
    {
        private BattleNetApiSettings _battleNetApiSettings;
        private BattleNetApiService _battleNetApiService;

        public BattleNetApiServiceTests()
        {
            //Set up configuration
            var builder = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("config.Test.json", optional: false, reloadOnChange: true)
                .AddJsonFile("config.Test.Local.json", optional: true, reloadOnChange: true);

            var config = builder.Build();
            _battleNetApiSettings = new BattleNetApiSettings();
            config.GetSection(nameof(BattleNetApiSettings)).Bind(_battleNetApiSettings);

            var logger = new Mock<ILogger<BattleNetApiService>>();

            _battleNetApiService = new BattleNetApiService(_battleNetApiSettings, logger.Object);
        }

        [Fact(Skip = "Integration test")]
        public void GetCharacterData_GettingCharacterData_CharacterDataReturned()
        {
            //Arrange
            var charName = "Veinlash";
            var realmName = "Karazhan";
            var region = "eu";

            //Act
            var result = _battleNetApiService.GetCharacterData(realmName, charName, region);

            //Assert
            Assert.True(result != null);
        }

        [Fact(Skip = "Integration test")]
        public void GetCharacterHeartOfAzerothLevel_GettingCharacterHeartOfAzerothLevel_HeartOfAzerothLevelIsAbove0()
        {
            //Arrange
            var charName = "Veinlash";
            var realmName = "Karazhan";
            var region = "eu";

            //Act
            var character = _battleNetApiService.GetCharacterData(realmName, charName, region);
            var hoaLevel = _battleNetApiService.GetCharacterHeartOfAzerothLevel(character);

            //Assert
            Assert.True(hoaLevel > 0); //This may no longer be the case after BfA
        }

        [Fact(Skip = "Integration test")]
        public void GetCharacterThumbnail_GettingCharacterThumbnail_ThumbnailExistsAndIsImageJpeg()
        {
            //Arrange
            var charName = "Veinlash";
            var realmName = "Karazhan";
            var region = "eu";

            //Act
            var character = _battleNetApiService.GetCharacterData(realmName, charName, region);
            var thumbnail = _battleNetApiService.GetCharacterThumbnail(region, realmName, charName);

            using (var httpClient = new HttpClient())
            {
                var thumbnailRequestResult = httpClient.GetAsync(thumbnail).GetAwaiter().GetResult();

                //Assert
                Assert.Equal(200, (int)thumbnailRequestResult.StatusCode);
                Assert.Equal("image/jpeg", thumbnailRequestResult.Content.Headers.ContentType.MediaType);
            }
        }

        [Fact(Skip = "Integration test")]
        public void Something()
        {
            _battleNetApiService.GetAuctionPrice("stuff");
        }
    }
}
