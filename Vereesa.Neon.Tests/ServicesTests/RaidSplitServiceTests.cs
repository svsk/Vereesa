using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Xunit;
using Moq;
using Vereesa.Neon.Integrations;
using Vereesa.Neon.Services;

namespace Vereesa.Core.Tests.ServicesTests
{
    public class RaidSplitServiceTests
    {
        [Fact]
        public async Task TestAsync()
        {
            // var warcraftLogsApi = new Mock<IWarcraftLogsApi>();
            // var discordMock = new Mock<DiscordSocketClient>();
            // var messageMock = new Mock<IMessage>();
            // string returnedMessage = null;

            // var service = new RaidSplitService(warcraftLogsApi.Object);

            // await service.SplitRaidEvenlyAsync(messageMock.Object, "2", "");

            // Assert.Equal("Fenriz, Eyrie, Azibo, Zyx, Xevlicious", returnedMessage);
        }
    }
}
