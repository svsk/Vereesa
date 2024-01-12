using Xunit;
using Vereesa.Neon.Integrations;
using Vereesa.Neon.Services;

namespace Vereesa.Core.Tests.ServicesTests
{
    public class TodayInWowServiceTests
    {
        [Fact]
        public void GenerateTodayInWow_GeneratingAndSendingEmbed_EmbedSentCorrectly()
        {
            var whclient = new WowheadClient();
            //var service = new TodayInWoWService(whclient);
        }
    }
}
