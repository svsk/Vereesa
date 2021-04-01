using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Vereesa.Core.Integrations;
using Vereesa.Core.Services;

namespace Vereesa.Core.Tests.ServicesTests
{
	[TestClass]
	public class RaidSplitServiceTests
	{
		[TestMethod]
		public async Task TestAsync()
		{
			var warcraftLogsApi = new Mock<IWarcraftLogsApi>();
			var discordMock = new Mock<DiscordSocketClient>();
			var messageMock = new Mock<IMessage>();
			string returnedMessage = null;
			messageMock.Setup(m => m.Channel.SendMessageAsync(It.IsAny<string>(), false, null, null, null, null))
				.Callback<string, bool, Embed, RequestOptions>((message, isTts, embed, options) =>
				{
					returnedMessage = message;
				});

			var service = new RaidSplitService(discordMock.Object, warcraftLogsApi.Object);

			await service.SplitRaidEvenlyAsync(messageMock.Object, "2");

			Assert.AreEqual("Fenriz, Eyrie, Azibo, Zyx, Xevlicious", returnedMessage);
		}
	}
}