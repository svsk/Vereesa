
using Vereesa.Core.Integrations;
using Xunit;

namespace Vereesa.Core.Tests.IntegrationTests
{

	public class WowheadClientTests
	{
		[Fact]
		public void GetTodayInWow_GettingTodayInWowFromWowhead_ReceivedCorrectly()
		{
			//Arrange
			var target = new WowheadClient();

			//Act
			var result = target.GetTodayInWow();

			//Assert
			Assert.NotNull(result);
		}
	}
}