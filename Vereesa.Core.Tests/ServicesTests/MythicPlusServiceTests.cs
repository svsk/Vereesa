// using Discord.WebSocket;
// using Microsoft.VisualStudio.TestTools.UnitTesting;
// using Moq;
// using Vereesa.Core.Configuration;
// using Vereesa.Core.Integrations;
// using Vereesa.Core.Integrations.Interfaces;
// using Vereesa.Core.Services;

// namespace Vereesa.Core.Tests.ServicesTests
// {
// 	[TestClass]
// 	public class MythicPlusServiceTests
// 	{
// 		private MythicPlusService _sut;

// 		[TestInitialize]
// 		public void SetUp()
// 		{
// 			var discord = new Mock<DiscordSocketClient>();
// 			//var googleSheetsClient = new Mock<ISpreadsheetClient>();
// 			var googleSheetsClient = new GoogleSheetsClient();
// 			var settings = new MythicPlusServiceSettings();
// 			_sut = new MythicPlusService(discord.Object, settings, googleSheetsClient);
// 		}

// 		[DataRow("Temple of Sethralis 15", "Temple of Sethralis", 15)]
// 		[DataRow("Temple of Sethralis +15", "Temple of Sethralis", 15)]
// 		[DataRow("+15 Temple of Sethralis", "Temple of Sethralis", 15)]
// 		[DataRow("Underrot 7", "Underrot", 7)]
// 		[DataTestMethod]
// 		public void ParseMessage_ParsesCorrectly(string message, string shouldBeDungeon, int shouldBeLevel)
// 		{
// 			var result = _sut.ParseMessage(message);

// 			Assert.AreEqual(shouldBeDungeon, result.Dungeon);
// 			Assert.AreEqual(shouldBeLevel, result.Level);
// 		}

// 		[TestMethod]
// 		public void GetKeys_GetsListOfKeys()
// 		{
// 			var result = _sut.GetKeys();
// 			Assert.IsNotNull(result);
// 		}

// 		[TestMethod]
// 		public void AddKey_AddsKeyToSpreadsheet()
// 		{
// 			_sut.AddKey("Veinlash", "Temple of Sethralis", 15);
// 		}
// 	}
// }