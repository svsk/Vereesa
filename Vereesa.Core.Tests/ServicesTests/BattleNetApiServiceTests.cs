using System;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Vereesa.Core.Configuration;
using Vereesa.Core.Services;

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

            _battleNetApiService = new BattleNetApiService(_battleNetApiSettings);
        }

        [TestMethod]
        public void TestGetCharacterStats()
        {
            //Arrange
            var charName = "Veinlash";
            var realmName = "Karazhan";
            var region = "eu";

            //Act
            var result = _battleNetApiService.GetCharacterData(realmName, charName, region).GetAwaiter().GetResult();

            //Assert
            Assert.IsTrue(result != null);
        }
    }
}
