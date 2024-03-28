using Vereesa.Neon.Integrations;
using Xunit;
using System.Net.Http;
using System.Threading.Tasks;
using System.Linq;

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
            var elementalStorms = eventsInEu.Groups
                .Where(grp => grp != null)
                .FirstOrDefault(grp => grp.Id == "elemental-storms");

            Assert.NotNull(elementalStorms);
        }

        [Fact]
        public async Task GetCurrentElementalStorm_GettingCurrentElementalStormFromWowhead_ReceivedCorrectly()
        {
            // Arrange
            var httpClient = new HttpClient();
            var target = new WowheadClient(httpClient);

            // Act
            var result = await target.GetCurrentElementalStorm();

            // Assert
            Assert.NotNull(result);
        }
    }
}
