using System;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Vereesa.Core.Services;

namespace Vereesa.Core.Tests.ServicesTests
{
	[TestClass]
	public class CharacterServiceTests
	{
		[TestMethod]
		public async Task Test()
		{
			var mockDiscord = new Mock<DiscordSocketClient>();
			var target = new CharacterService(mockDiscord.Object, null);

			var messageMock = new Mock<IMessage>();
			var channelMock = new Mock<IMessageChannel>();
			channelMock.Setup(c => c.SendMessageAsync(It.IsAny<string>(), false, null, null))
				.Callback<string, bool, Embed, RequestOptions>((msg, isTts, embed, opt) =>
				{
					Console.WriteLine(msg);
				});

			messageMock.Setup(m => m.Content).Returns("!claim Veinlash-Karazhan");
			messageMock.Setup(m => m.Channel).Returns(channelMock.Object);

			await target.HandleClaimCommandAsync(messageMock.Object, "Veinlash-Karazhan");

			// Assert.IsTrue(called);
		}
	}
}