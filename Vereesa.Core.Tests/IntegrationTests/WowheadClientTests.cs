using Microsoft.VisualStudio.TestTools.UnitTesting;
using Vereesa.Core.Integrations;

namespace Vereesa.Core.Tests.IntegrationTests
{
    [TestClass]
    public class WowheadClientTests
    {
        [TestMethod]
        public void GetTodayInWow_GettingTodayInWowFromWowhead_ReceivedCorrectly() 
        {
            //Arrange
            var target  = new WowheadClient();

            //Act
            var result = target.GetTodayInWow();

            //Assert
            Assert.IsNotNull(result);
        }
    }
}