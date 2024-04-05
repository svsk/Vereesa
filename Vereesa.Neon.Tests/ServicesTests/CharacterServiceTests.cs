using System;
using System.Threading.Tasks;
using Discord;
using Xunit;
using Moq;
using Vereesa.Neon.Services;

namespace Vereesa.Core.Tests.ServicesTests
{
    public class CharacterServiceTests
    {
        [Fact(Skip = "Not done")]
        public async Task Test()
        {
            var mockMessaging = new Mock<IMessagingClient>();
            var target = new CharacterService(mockMessaging.Object, null);

            var messageMock = new Mock<IMessage>();
            var channelMock = new Mock<IMessageChannel>();
            channelMock
                .Setup(
                    c =>
                        c.SendMessageAsync(
                            It.IsAny<string>(),
                            false,
                            null,
                            null,
                            null,
                            null,
                            null,
                            null,
                            null,
                            MessageFlags.None
                        )
                )
                .Callback<string, bool, Embed, RequestOptions>(
                    (msg, isTts, embed, opt) =>
                    {
                        Console.WriteLine(msg);
                    }
                );

            messageMock.Setup(m => m.Content).Returns("!claim Veinlash-Karazhan");
            messageMock.Setup(m => m.Channel).Returns(channelMock.Object);

            await target.HandleClaimCommandAsync(messageMock.Object, "Veinlash-Karazhan");

            // Assert.True(called);
        }
    }
}
