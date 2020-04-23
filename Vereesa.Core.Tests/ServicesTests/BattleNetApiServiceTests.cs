using System;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Vereesa.Core.Configuration;
using Vereesa.Core.Services;
using Vereesa.Core.Tests.Mocks;

namespace Vereesa.Core.Tests.ServicesTests
{
    [TestClass]
    public class BattleNetApiServiceTests
    {
        private BattleNetApiSettings _battleNetApiSettings;
        private BattleNetApiService _battleNetApiService;

        [TestInitialize]
        public void Before()
        {
            //Set up configuration
            var builder = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("config.Test.json", optional: false, reloadOnChange: true)
                .AddJsonFile("config.Test.Local.json", optional: true, reloadOnChange: true);

            var config = builder.Build();
            _battleNetApiSettings = new BattleNetApiSettings();
            config.GetSection(nameof(BattleNetApiSettings)).Bind(_battleNetApiSettings);

            _battleNetApiService = new BattleNetApiService(_battleNetApiSettings, new ILoggerMock<BattleNetApiService>());
        }

        [TestMethod]
        public void GetCharacterData_GettingCharacterData_CharacterDataReturned()
        {
            //Arrange
            var charName = "Veinlash";
            var realmName = "Karazhan";
            var region = "eu";

            //Act
            var result = _battleNetApiService.GetCharacterData(realmName, charName, region);

            //Assert
            Assert.IsTrue(result != null);
        }

        [TestMethod]
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
            Assert.IsTrue(hoaLevel > 0); //This may no longer be the case after BfA
        }

        [TestMethod]
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
                Assert.AreEqual(200, (int)thumbnailRequestResult.StatusCode);
                Assert.AreEqual("image/jpeg", thumbnailRequestResult.Content.Headers.ContentType.MediaType);
            }
        }

        [TestMethod]
        public void Something() 
        {
            _battleNetApiService.GetAuctionPrice("stuff");
        }
    }
}
