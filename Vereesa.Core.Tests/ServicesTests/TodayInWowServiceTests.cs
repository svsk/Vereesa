using Microsoft.VisualStudio.TestTools.UnitTesting;
using Vereesa.Core.Integrations;
using Vereesa.Core.Services;

namespace Vereesa.Core.Tests.ServicesTests
{
    [TestClass]
    public class TodayInWowServiceTests
    {
        [TestMethod]
        public void GenerateTodayInWow_GeneratingAndSendingEmbed_EmbedSentCorrectly() 
        {
            var whclient = new WowheadClient();
            //var service = new TodayInWoWService(whclient);
        }
    }
}