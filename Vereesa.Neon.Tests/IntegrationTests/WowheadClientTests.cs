using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Vereesa.Neon.Integrations;
using Xunit;

namespace Vereesa.Neon.Tests.IntegrationTests
{
    public class WowheadClientTests
    {
        [Fact]
        public async Task GetTodayInWow_GettingTodayInWowFromWowhead_ReceivedCorrectly()
        {
            // Arrange
            var httpClient = new HttpClient();
            var target = new WowheadClient(httpClient);

            // Act
            var result = await target.GetTodayInWow();

            // Assert
            var eventsInEu = result.FirstOrDefault(grp => grp.Id == "events-and-rares" && grp.RegionId == "EU");

            Assert.NotNull(eventsInEu);
            Assert.NotNull(eventsInEu.Groups);
            Assert.NotEmpty(eventsInEu.Groups);
        }

        [Fact]
        public async Task GetCurrentElementalStorm_GettingCurrentElementalStormFromWowhead_ReceivedCorrectly()
        {
            // Arrange
            var httpClient = new HttpClient();
            var target = new WowheadClient(httpClient);

            // Act
            var result = await target.GetCurrentElementalStorms();

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetCurrentGrandHunt_GettingCurrentGrandHuntFromWowhead_ReceivedCorrectly()
        {
            // Arrange
            var httpClient = new HttpClient();
            var target = new WowheadClient(httpClient);

            // Act
            var result = await target.GetCurrentGrandHunts();

            // Assert
            Assert.NotNull(result);
        }

        [Fact(Skip = "Radiant echoes are no longer active.")]
        public async Task GetRadiantEchoesEvents_GettingCurrentRadiantEchoesEventsFromWowhead_ReceivedCorrectly()
        {
            // Arrange
            var httpClient = new HttpClient();
            var target = new WowheadClient(httpClient);

            // Act
            var result = await target.GetCurrentRadiantEchoesEvents();

            // Assert
            Assert.NotNull(result);
        }
    }
}
