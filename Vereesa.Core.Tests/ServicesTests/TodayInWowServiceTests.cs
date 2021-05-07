using Xunit;
using Vereesa.Core.Integrations;
using Vereesa.Core.Services;

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